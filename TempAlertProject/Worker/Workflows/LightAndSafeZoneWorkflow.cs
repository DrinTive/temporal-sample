using Temporalio.Workflows;
using Worker.Activities;

namespace Worker.Workflows;

/// <summary>
/// SOP: https://www.figma.com/board/aqefrxJU5ce5Z0T64KEOOs/Sample-Workflow?node-id=110-190&p=f&t=KyJNkdyHDm300edM-0 
/// </summary>
[Workflow]
public class LightAndSafeZoneWorkflow
{
    private double _lightMaxThreshold;
    private double? _lightLevel;
    private bool? _isInSafeZone;
    private bool? _sealBroken;

    private TaskCompletionSource _triggerReady = new();
    private TaskCompletionSource _responseSignal = new();

    [WorkflowRun]
    public async Task<bool?> RunAsync(double lightMaxThreshold, double batteryMinThreshold)
    {
        _lightMaxThreshold = lightMaxThreshold;
        
        // Wait for the trigger condition (light above threshold and not in safe zone)
        await WaitForTriggerConditionAsync();

        // Execute activities
        await Workflow.ExecuteActivityAsync(
            () => Actions.UpdateTransmissionIntervalAsync(5),
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(20) });

        await Workflow.ExecuteActivityAsync(
            () => Actions.ActivateGpsAsync(),
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(20) });

        await Workflow.ExecuteActivityAsync(
            () => Actions.SendEmailAsync("example@tive.com", "TemplateX"),
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(20) });

        // Start the child workflow (BatteryControlWorkflow)
        await Workflow.StartChildWorkflowAsync<BatteryControlWorkflow>(
            wf => wf.RunAsync(batteryMinThreshold),
            new ChildWorkflowOptions
            {
                Id = "battery-control-child",
                TaskQueue = "Temp_alert_task_queue"
            });

        // Prepare to wait for response or timeout
        _responseSignal = new TaskCompletionSource(); // reset before waiting
        using var cts = new CancellationTokenSource();

        var responseTask = WaitForResponseAsync(cts.Token);
        var timeoutTask = Workflow.DelayAsync(TimeSpan.FromMinutes(5), cts.Token);

        var completed = await Task.WhenAny(responseTask, timeoutTask);

        if (completed == responseTask)
        {
            cts.Cancel();  // cancel timeout if response received
        }
        else
        {
            // Timeout expired, call escalation activity
            await Workflow.ExecuteActivityAsync(
                () => Actions.CallEscalationContactAsync(),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(20) });

            return null; // or false/null indicating no response
        }

        if (_sealBroken == true)
        {
            // Seal broken, escalate
            await Workflow.ExecuteActivityAsync(
                () => Actions.CallEscalationContactAsync(),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(20) });
        }
        else
        {
            // Seal intact, perform cleanup activities
            await Workflow.ExecuteActivityAsync(
                () => Actions.CloseStatusAsync(),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(20) });

            await Workflow.ExecuteActivityAsync(
                () => Actions.RevertTransmissionSettingsAsync(),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(20) });

            await Workflow.ExecuteActivityAsync(
                () => Actions.DeactivateGpsAsync(),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(20) });
        }

        // Return seal status (true if broken, false if intact)
        return _sealBroken;
    }

    private async Task WaitForTriggerConditionAsync()
    {
        while (true)
        {
            await _triggerReady.Task;

            if (_lightLevel.HasValue && _isInSafeZone.HasValue && _lightLevel > _lightMaxThreshold && !_isInSafeZone.Value)
            { 
                return;
            }

            _triggerReady = new TaskCompletionSource();
        }
    }

    [WorkflowSignal]
    public Task SubmitLightReading(double light)
    {
        _lightLevel = light;
        _triggerReady.TrySetResult();
        return Task.CompletedTask;
    }

    [WorkflowSignal]
    public Task SubmitSafeZoneStatus(bool isSafe)
    {
        _isInSafeZone = isSafe;
        _triggerReady.TrySetResult();
        return Task.CompletedTask;
    }

    [WorkflowSignal]
    public Task AcknowledgeResponse(bool sealBroken)
    {
        _sealBroken = sealBroken;
        if (!_responseSignal.Task.IsCompleted)
            _responseSignal.TrySetResult();
        return Task.CompletedTask;
    }

    private async Task WaitForResponseAsync(CancellationToken token)
    {
        await using (token.Register(() => _responseSignal.TrySetCanceled()))
        {
            await _responseSignal.Task;
        }
    }
}