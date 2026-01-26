using System.Management;

namespace BatteryNotificationService;

/// <summary>
/// Background service that monitors the power status of the device and sends notifications
/// when the power source changes (AC/Battery).
/// </summary>
public class Worker(ILogger<Worker> logger) : BackgroundService
{
    private ManagementEventWatcher? _watcher;
    private bool _wasPluggedIn = IsPluggedIn();

    /// <summary>
    /// Executes the background service logic.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token that indicates when the service should stop.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Battery Notification Service iniciado em: {time}",
            DateTimeOffset.Now
        );

        // Initial notification
        ShowNotification(
            _wasPluggedIn ? "Conectado na tomada" : "Usando bateria",
            $"Serviço iniciado. Status atual: {(_wasPluggedIn ? "AC" : "Bateria")}"
        );

        // Monitor power status changes
        StartPowerMonitoring(stoppingToken);

        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    /// <summary>
    /// Initializes and starts the WMI event watcher for power management events.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    private void StartPowerMonitoring(CancellationToken stoppingToken)
    {
        try
        {
            // WMI query to detect power status changes
            WqlEventQuery query = new("SELECT * FROM Win32_PowerManagementEvent");
            _watcher = new ManagementEventWatcher(query);

            _watcher.EventArrived += (sender, args) =>
            {
                if (stoppingToken.IsCancellationRequested)
                    return;
                CheckPowerStatus();
            };

            _watcher.Start();
            logger.LogInformation("Monitoramento de energia ativado");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao iniciar monitoramento de energia");
        }
    }

    /// <summary>
    /// Checks the current power status and sends a notification if it has changed since the last check.
    /// </summary>
    private void CheckPowerStatus()
    {
        try
        {
            bool isCurrentlyPluggedIn = IsPluggedIn();

            // Detect state change
            if (isCurrentlyPluggedIn != _wasPluggedIn)
            {
                _wasPluggedIn = isCurrentlyPluggedIn;

                if (isCurrentlyPluggedIn)
                {
                    logger.LogInformation("Notebook conectado na tomada");
                    ShowNotification("⚡ Conectado na tomada", "Seu notebook está sendo carregado");
                }
                else
                {
                    logger.LogInformation("Notebook desconectado da tomada");
                    ShowNotification(
                        "🔋 Usando bateria",
                        "Seu notebook foi desconectado da tomada"
                    );
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao verificar status de energia");
        }
    }

    /// <summary>
    /// Determines whether the device is currently plugged into a power source.
    /// </summary>
    /// <returns>True if plugged in (AC online), otherwise false.</returns>
    private static bool IsPluggedIn()
    {
        var status = SystemInformation.PowerStatus;
        return status.PowerLineStatus == PowerLineStatus.Online;
    }

    /// <summary>
    /// Displays a Windows toast notification.
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message body of the notification.</param>
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

            logger.LogInformation("Notificação exibida: {title}", title);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao exibir notificação");
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
