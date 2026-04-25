using System.Windows.Forms;

namespace UrlMonitorWorker;

public class ServiceDebuggerForm : Form
{
    private readonly IHost _host;
    private Button _startButton;
    private Button _stopButton;
    private TextBox _logTextBox;
    private Task? _hostTask;
    private CancellationTokenSource? _cts;

    public ServiceDebuggerForm(IHost host)
    {
        _host = host;
        InitializeUI();
    }

    private void InitializeUI()
    {
        Text = "Worker Service Debugger";
        Size = new System.Drawing.Size(600, 400);
        StartPosition = FormStartPosition.CenterScreen;

        _startButton = new Button
        {
            Text = "Start Service",
            Location = new System.Drawing.Point(10, 10),
            Size = new System.Drawing.Size(100, 30)
        };
        _startButton.Click += StartButton_Click;

        _stopButton = new Button
        {
            Text = "Stop Service",
            Location = new System.Drawing.Point(120, 10),
            Size = new System.Drawing.Size(100, 30),
            Enabled = false
        };
        _stopButton.Click += StopButton_Click;

        _logTextBox = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new System.Drawing.Point(10, 50),
            Size = new System.Drawing.Size(560, 300),
            ReadOnly = true
        };

        Controls.Add(_startButton);
        Controls.Add(_stopButton);
        Controls.Add(_logTextBox);

        FormClosing += ServiceDebuggerForm_FormClosing;
    }

    private async void StartButton_Click(object? sender, EventArgs e)
    {
        _startButton.Enabled = false;
        _stopButton.Enabled = true;
        AppendLog("Starting service...");

        _cts = new CancellationTokenSource();
        _hostTask = _host.StartAsync(_cts.Token);
        AppendLog("Service started.");
    }

    private async void StopButton_Click(object? sender, EventArgs e)
    {
        _stopButton.Enabled = false;
        AppendLog("Stopping service...");

        _cts?.Cancel();
        await _host.StopAsync();

        if (_hostTask != null)
        {
            await _hostTask;
        }

        AppendLog("Service stopped.");
        _startButton.Enabled = true;
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => AppendLog(message));
            return;
        }

        _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private async void ServiceDebuggerForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_hostTask != null && !_hostTask.IsCompleted)
        {
            _cts?.Cancel();
            await _host.StopAsync();
        }

        _host.Dispose();
    }
}