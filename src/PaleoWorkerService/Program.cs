using System.Net;
using System.Windows.Forms;
using UrlMonitorWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddHostedService<Worker>();

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
