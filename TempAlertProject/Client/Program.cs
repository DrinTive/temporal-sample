using System;
using Client.Clients;

Console.WriteLine("Choose client to load:");
Console.WriteLine("1. LightAndSafeZoneClient");
Console.WriteLine("2. TemperatureEscalationClient");
Console.Write("Enter choice (1 or 2): ");

var choice = Console.ReadLine();

switch (choice)
{
    case "1":
        await LightAndSafeZoneClient.InitializeClient();
        Console.WriteLine("LightAndSafeZoneClient initialized.");
        break;

    case "2":
        await TemperatureEscalationClient.InitializeClient();
        Console.WriteLine("TemperatureEscalationClient initialized.");
        break;

    default:
        Console.WriteLine("Invalid choice.");
        break;
}