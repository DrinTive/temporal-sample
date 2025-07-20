using System;
using System.Threading.Tasks;
using Temporalio.Client;
using Worker.Workflows;

namespace Client.Clients
{
    public static class TemperatureEscalationClient
    {
        public static async Task InitializeClient()
        {
        
            TemporalClient client = await TemporalClient.ConnectAsync(new("localhost:7233") { Namespace = "default" });

            var workflowId = $"temp-alert-{Guid.NewGuid()}";

// The minimum temperature increase (in degrees) to trigger the alert
            const double thresholdDelta = 5.0;

// The time window within which the temperature increase must occur to be considered significant
            TimeSpan thresholdTimeWindow = TimeSpan.FromSeconds(15);

// The time (in minutes) to wait for a response from the transporter before escalating the issue
            TimeSpan responseWaitWindow = TimeSpan.FromSeconds(20);

            WorkflowHandle<TemperatureEscalationWorkflow> handle = await client.StartWorkflowAsync(
                (TemperatureEscalationWorkflow wf) => wf.RunAsync(thresholdDelta, thresholdTimeWindow, responseWaitWindow),
                new WorkflowOptions(id: workflowId, taskQueue: "Temp_alert_task_queue"));

            Console.WriteLine($"Started workflow with ID: {workflowId}");
            Console.WriteLine("Commands:");
            Console.WriteLine(" - Type a number to submit temperature reading");
            Console.WriteLine(" - 'ack' to acknowledge response");
            Console.WriteLine(" - 'exit' to quit");

            while (true)
            {
                string? input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input)) continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Exiting.");

                    break;
                }
                else if (input.Equals("ack", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await handle.SignalAsync(wf => wf.AcknowledgeResponse());
                        Console.WriteLine("Acknowledgement signal sent.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send ack signal: {ex.Message}");
                    }
                }
                else if (double.TryParse(input, out double tempReading))
                {
                    try
                    {
                        await handle.SignalAsync(wf => wf.SubmitTemperatureReading(tempReading));
                        Console.WriteLine($"Sent temperature reading: {tempReading}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send temperature reading: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Unknown command: {input}");
                }
            }

        }
    }
}