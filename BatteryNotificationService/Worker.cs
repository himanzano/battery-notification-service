using System.Management;

namespace BatteryNotificationService;

/// <summary>
/// Background worker that monitors the device's power status and triggers
/// Windows toast notifications when the power source changes (e.g., AC to Battery).
/// </summary>
public class Worker(ILogger<Worker> logger) : BackgroundService
{
    private ManagementEventWatcher? _watcher;
    private bool _wasPluggedIn = IsPluggedIn();

    /// <summary>
    /// Starts the background execution logic for power monitoring.
    /// </summary>
    /// <param name="stoppingToken">Triggered when the application is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Battery Notification Service started at: {time}",
            DateTimeOffset.Now
        );

        // Send initial notification with current status
        ShowNotification(
            _wasPluggedIn ? "Conectado na tomada" : "Usando bateria",
            $"Serviço iniciado. Status atual: {(_wasPluggedIn ? "AC" : "Bateria")}"
        );

        // Initialize WMI monitoring
        StartPowerMonitoring(stoppingToken);

        // Keep the task alive until cancellation is requested
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    /// <summary>
    /// Sets up and starts a WMI event watcher to listen for Win32_PowerManagementEvent occurrences.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    private void StartPowerMonitoring(CancellationToken stoppingToken)
    {
        try
        {
            // Use WMI to detect hardware-level power events
            WqlEventQuery query = new("SELECT * FROM Win32_PowerManagementEvent");
            _watcher = new ManagementEventWatcher(query);

            _watcher.EventArrived += (sender, args) =>
            {
                if (stoppingToken.IsCancellationRequested)
                    return;

                CheckPowerStatus();
            };

            _watcher.Start();
            logger.LogInformation("Power status monitoring activated.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start power monitoring.");
        }
    }

    /// <summary>
    /// Compares the current power status with the last known state and triggers notifications on changes.
    /// </summary>
    private void CheckPowerStatus()
    {
        try
        {
            bool isCurrentlyPluggedIn = IsPluggedIn();

            // Notify only if the state has changed (e.g., plugged in -> unplugged)
            if (isCurrentlyPluggedIn != _wasPluggedIn)
            {
                _wasPluggedIn = isCurrentlyPluggedIn;

                if (isCurrentlyPluggedIn)
                {
                    logger.LogInformation("Device connected to AC power.");
                    ShowNotification("⚡ Conectado na tomada", "Seu notebook está sendo carregado");
                }
                else
                {
                    logger.LogInformation("Device disconnected from AC power (using battery).");
                    ShowNotification(
                        "🔋 Usando bateria",
                        "Seu notebook foi desconectado da tomada"
                    );
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while checking power status.");
        }
    }

    /// <summary>
    /// Queries the system to determine if the device is currently running on AC power.
    /// </summary>
    /// <returns>True if the PowerLineStatus is Online (AC); otherwise, false.</returns>
    private static bool IsPluggedIn()
    {
        var status = SystemInformation.PowerStatus;
        return status.PowerLineStatus == PowerLineStatus.Online;
    }

    /// <summary>
    /// Generates and displays a native Windows Toast Notification using XML-based templates.
    /// </summary>
    /// <param name="title">The notification header.</param>
    /// <param name="message">The notification body content.</param>
    private void ShowNotification(string title, string message)
    {
        try
        {
            var xmlString =
                $@"
<toast>
    <visual>
        <binding template='ToastText02'>
            <text id='1'>{System.Security.SecurityElement.Escape(title)}</text>
            <text id='2'>{System.Security.SecurityElement.Escape(message)}</text>
        </binding>
    </visual>
</toast>";

            var xmlDoc = new Windows.Data.Xml.Dom.XmlDocument();
            xmlDoc.LoadXml(xmlString);

            var toast = new Windows.UI.Notifications.ToastNotification(xmlDoc);
            Windows
                .UI.Notifications.ToastNotificationManager.CreateToastNotifier(
                    "BatteryNotificationService"
                )
                .Show(toast);

            logger.LogInformation("Notification displayed: {title}", title);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to display toast notification.");
        }
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        _watcher?.Stop();
        _watcher?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
