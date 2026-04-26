using Microsoft.Extensions.Logging.EventLog;
using System.Diagnostics;
using System.Windows.Forms;
using UrlMonitorWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (OperatingSystem.IsWindows())
{
    builder.Logging.AddEventLog(new EventLogSettings
    {
        SourceName = "PaleoWorkerService"
    });
}

// Ensure Event Source exists (Windows only)
if (OperatingSystem.IsWindows())
{
    const string eventSource = "PaleoWorkerService";
    const string logName = "Application";
    if (!EventLog.SourceExists(eventSource))
    {
        // Creating an event source requires admin rights
        EventLog.CreateEventSource(eventSource, logName);
        Console.WriteLine($"Event source '{eventSource}' created. Please restart the application.");
        return; // Exit so the user can restart after source creation
    }
}


var host = builder.Build();

if (builder.Environment.IsDevelopment())
{
    var serviceController = new ServiceDebuggerForm(host);
    Application.Run(serviceController);
}
else
{
    host.Run();
}
