using Microsoft.Extensions.Logging.EventLog;
using System.Diagnostics;
using System.Windows.Forms;
using UrlMonitorWorker;

var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Worker>();
    })
    .ConfigureLogging((hostingContext, logging) =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        if (OperatingSystem.IsWindows())
        {
            logging.AddEventLog(new EventLogSettings
            {
                SourceName = "PaleoWorkerService"
            });
        }
    });

// Ensure Event Source exists (Windows only)
if (OperatingSystem.IsWindows())
{
    const string eventSource = "PaleoWorkerService";
    const string logName = "Application";
    if (!EventLog.SourceExists(eventSource))
    {
        EventLog.CreateEventSource(eventSource, logName);
        Console.WriteLine($"Event source '{eventSource}' created. Please restart the application.");
        return;
    }
}

var host = builder.Build();

if (host.Services.GetRequiredService<IHostEnvironment>().IsDevelopment())
{
    var serviceController = new ServiceDebuggerForm(host);
    Application.Run(serviceController);
}
else
{
    host.Run();
}
