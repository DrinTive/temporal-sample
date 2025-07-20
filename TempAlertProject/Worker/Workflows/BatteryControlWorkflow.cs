using Temporalio.Workflows;
using Worker.Activities;

namespace Worker.Workflows;

[Workflow]
public class BatteryControlWorkflow
{
    private double? _batteryLevel;
    private TaskCompletionSource _readingReceived = new();

    [WorkflowRun]
    public async Task RunAsync(double thresholdPercentage)
    {
        await WaitForConditionAsync(() => _batteryLevel < thresholdPercentage);

        await Workflow.ExecuteActivityAsync(() => Actions.UpdateTransmissionIntervalAsync(20),
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(20) });
    }

    private async Task WaitForConditionAsync(Func<bool> condition)
    {
        while (!condition())
        {
            await _readingReceived.Task;
            _readingReceived = new TaskCompletionSource();
        }
    }

    [WorkflowSignal]
    public Task SubmitBatteryReading(double battery)
    {
        _batteryLevel = battery;
        _readingReceived.TrySetResult();
        return Task.CompletedTask;
    }
}