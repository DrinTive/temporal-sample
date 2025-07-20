using System.Diagnostics;
using Temporalio.Common;
using Temporalio.Workflows;
using Worker.Activities;

namespace Worker.Workflows
{
    /// <summary>
    /// SOP: https://www.figma.com/board/aqefrxJU5ce5Z0T64KEOOs/Sample-Workflow?node-id=60-393&p=f&t=KyJNkdyHDm300edM-0
    /// </summary>
    [Workflow]
    public class TemperatureEscalationWorkflow
    {
        private readonly List<(double Temperature, DateTime Timestamp)> _readings = new();

        // This holds the current waiting task; start with a new one
        private TaskCompletionSource _newReadingSignal = new();

        private readonly TaskCompletionSource _responseSignal = new();

        [WorkflowRun]
        public async Task RunAsync(double thresholdDelta, TimeSpan thresholdTimeWindow, TimeSpan responseWaitWindow)
        {
            while (true)
            {
                if (_readings.Count == 0)
                {
                    Debug.WriteLine("Waiting for first temperature reading...");
                    await WaitForNewReadingAsync();
                }

                (double Temperature, DateTime Timestamp) latest = _readings.Last();

                // Find any reading within the time window before latest
                DateTime windowStart = latest.Timestamp - thresholdTimeWindow;
                (double Temperature, DateTime Timestamp) candidate = _readings
                    .Where(r => r.Timestamp >= windowStart)
                    .OrderBy(r => r.Timestamp)
                    .FirstOrDefault();

                if (candidate != default)
                {
                    double delta = latest.Temperature - candidate.Temperature;
                    if (delta >= thresholdDelta)
                    {
                        Debug.WriteLine($"Temperature increased by {delta}° within {thresholdTimeWindow.TotalMinutes} minutes");
                        break;
                    }
                }

                Debug.WriteLine($"No significant increase detected yet (latest: {latest.Temperature}°). Waiting for next reading...");
                await WaitForNewReadingAsync();
            }

            // Step 2: Email transporter
            await Workflow.ExecuteActivityAsync(
                () => Actions.EmailTransporterActivity("transporter@tive.com"),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(20) });

            // Step 3: Wait for transporter response or timeout
            bool responseReceived = false;
            using CancellationTokenSource cts = new();
            Task responseTask = WaitForResponseAsync(cts.Token);
            Task timeoutTask = Workflow.DelayAsync(responseWaitWindow, cts.Token);
            Task completed = await Task.WhenAny(responseTask, timeoutTask);

            if (completed == responseTask)
            {
                responseReceived = true;
                await cts.CancelAsync();
            }

            if (responseReceived)
            {
                Debug.WriteLine("Response received from transporter, closing issue.");
                await Workflow.ExecuteActivityAsync(
                    () => Actions.CloseIssueActivity(),
                    new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(10) });
                return;
            }

            // Step 4: Escalation
            Debug.WriteLine("No response received; escalating issue.");

            string[] escalationEmails = ["emma@example.com", "dawn@example.com", "bo@example.com"];
            string[] escalationPhones = ["+15550001111", "+15550002222", "+15550003333"];
            const string transporterPhoneNumber = "+15551234567";

            await Workflow.ExecuteActivityAsync(
                () => Actions.NotifyEscalationGroupActivity(escalationEmails),
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromSeconds(30),
                    RetryPolicy = new RetryPolicy
                    {
                        InitialInterval = TimeSpan.FromSeconds(5),
                        MaximumInterval = TimeSpan.FromSeconds(30),
                        MaximumAttempts = 3
                    }
                });


            await Workflow.ExecuteActivityAsync(
                () => Actions.TextEscalationGroupActivity(escalationPhones),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(30) });

            await Workflow.ExecuteActivityAsync(
                () => Actions.CallTransporterActivity(transporterPhoneNumber),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(30) });
        }

        [WorkflowSignal]
        public Task SubmitTemperatureReading(double temperature)
        {
            _readings.Add((temperature, Workflow.UtcNow));

            // Only set result if waiting (not completed)
            if (!_newReadingSignal.Task.IsCompleted)
            {
                _newReadingSignal.TrySetResult();
            }

            return Task.CompletedTask;
        }

        private async Task WaitForNewReadingAsync()
        {
            // Await the current TCS task
            await _newReadingSignal.Task;

            // Reset _newReadingSignal after awaiting so next await can wait on new signal
            _newReadingSignal = new TaskCompletionSource();
        }

        [WorkflowSignal]
        public Task AcknowledgeResponse()
        {
            if (!_responseSignal.Task.IsCompleted)
            {
                _responseSignal.SetResult();
            }
            return Task.CompletedTask;
        }


        [WorkflowQuery]
        public List<(double Temperature, DateTime Timestamp)> GetCurrentReadings()
        {
            return _readings.ToList();
        }

        private async Task WaitForResponseAsync(CancellationToken token)
        {
            await using (token.Register(() => _responseSignal.TrySetCanceled()))
            {
                await _responseSignal.Task;
            }
        }
    }
}
