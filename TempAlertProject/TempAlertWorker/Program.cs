// @@@SNIPSTART money-transfer-project-template-dotnet-worker
// This file is designated to run the worker

using TempAlertWorker.Activities;
using TempAlertWorker.Workflows;
using Temporalio.Client;
using Temporalio.Worker;

// Create a client to connect to localhost on "default" namespace
TemporalClient client = await TemporalClient.ConnectAsync(new("localhost:7233"));

// Cancellation token to shutdown worker on ctrl+c
using CancellationTokenSource tokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    tokenSource.Cancel();
    eventArgs.Cancel = true;
};

// Create an instance of the activities since we have instance activities.
// If we had all static activities, we could just reference those directly.
TempAlertActivities activities = new TempAlertActivities();

// Create a worker with the activity and workflow registered
using TemporalWorker worker = new TemporalWorker(
    client, // client
    new TemporalWorkerOptions(taskQueue: "Temp_alert_task_queue")
        .AddAllActivities(activities)         // Register activities
        .AddWorkflow<TempAlertWorkflow>() // Register workflow
);

// Run the worker until it's cancelled
Console.WriteLine("Running worker...");
try
{
    await worker.ExecuteAsync(tokenSource.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Worker cancelled");
}
// @@@SNIPEND
