using System;
using System.Threading.Tasks;
using Temporalio.Client;
using Worker.Workflows;

namespace Client.Clients
{
    public static class LightAndSafeZoneClient
    {
        public static async Task InitializeClient()
        {
            TemporalClient client = await TemporalClient.ConnectAsync(new("localhost:7233") 
            { 
                Namespace = "default" 
            });

            var workflowId = $"light-safezone-{Guid.NewGuid()}";

            const double lightMaxThreshold = 100.0;     // example threshold
            const double batteryMinThreshold = 20.0;    // example threshold

            // Start the workflow
            WorkflowHandle<LightAndSafeZoneWorkflow, bool?> handle = await client.StartWorkflowAsync(
                (LightAndSafeZoneWorkflow wf) => wf.RunAsync(lightMaxThreshold, batteryMinThreshold),
                new WorkflowOptions(id: workflowId, taskQueue: "Temp_alert_task_queue"));

            WorkflowHandle<BatteryControlWorkflow> batteryChildHandle = client.GetWorkflowHandle<BatteryControlWorkflow>("battery-control-child");

            Console.WriteLine($"Started workflow with ID: {workflowId}");
            Console.WriteLine("Commands:");
            Console.WriteLine(" - Type 'light <value>' to submit light reading");
            Console.WriteLine(" - Type 'safe <true|false>' to submit safe zone status");
            Console.WriteLine(" - Type 'ack <true|false>' to acknowledge response (seal broken)");
            Console.WriteLine(" - Type 'battery <value>' to submit batter reading");
            Console.WriteLine(" - Type 'exit' to quit");

            while (true)
            {
                string? input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input)) continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Exiting.");
                    break;
                }

                var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    Console.WriteLine("Invalid command format.");
                    continue;
                }

                string command = parts[0].ToLower();
                string arg = parts[1];

                try
                {
                    switch (command)
                    {
                        case "light":
                            if (double.TryParse(arg, out double lightValue))
                            {
                                await handle.SignalAsync(wf => wf.SubmitLightReading(lightValue));
                                Console.WriteLine($"Sent light reading: {lightValue}");
                            }
                            else
                            {
                                Console.WriteLine("Invalid light value.");
                            }
                            break;
                        
                        case "battery":
                            if (double.TryParse(arg, out double batteryValue))
                            {
                                await batteryChildHandle.SignalAsync(wf => wf.SubmitBatteryReading(batteryValue));
                                Console.WriteLine($"Sent battery reading: {batteryValue}");
                            }
                            else
                            {
                                Console.WriteLine("Invalid battery value.");
                            }
                            break;

                        case "safe":
                            if (bool.TryParse(arg, out bool isSafe))
                            {
                                await handle.SignalAsync(wf => wf.SubmitSafeZoneStatus(isSafe));
                                Console.WriteLine($"Sent safe zone status: {isSafe}");
                            }
                            else
                            {
                                Console.WriteLine("Invalid safe zone value. Use true or false.");
                            }
                            break;

                        case "ack":
                            if (bool.TryParse(arg, out bool sealBroken))
                            {
                                await handle.SignalAsync(wf => wf.AcknowledgeResponse(sealBroken));
                                Console.WriteLine($"Sent acknowledgement (seal broken = {sealBroken}).");
                            }
                            else
                            {
                                Console.WriteLine("Invalid ack value. Use true or false.");
                            }
                            break;

                        default:
                            Console.WriteLine($"Unknown command: {command}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send signal: {ex.Message}");
                }
            }
        }
    }
}