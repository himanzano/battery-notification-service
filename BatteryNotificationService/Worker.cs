using System.Management;

namespace BatteryNotificationService;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    private ManagementEventWatcher? _watcher;
    private bool _wasPluggedIn = IsPluggedIn();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Battery Notification Service iniciado em: {time}",
            DateTimeOffset.Now
        );

        // Notificação inicial
        ShowNotification(
            _wasPluggedIn ? "Conectado na tomada" : "Usando bateria",
            $"Serviço iniciado. Status atual: {(_wasPluggedIn ? "AC" : "Bateria")}"
        );

        // Monitora mudanças no status de energia
        StartPowerMonitoring(stoppingToken);

        // Mantém o serviço rodando
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private void StartPowerMonitoring(CancellationToken stoppingToken)
    {
        try
        {
            // Query WMI para detectar mudanças no status de energia
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

    private void CheckPowerStatus()
    {
        try
        {
            bool isCurrentlyPluggedIn = IsPluggedIn();

            // Detecta mudança de estado
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

    private static bool IsPluggedIn()
    {
        var status = SystemInformation.PowerStatus;
        return status.PowerLineStatus == PowerLineStatus.Online;
    }

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

    public override void Dispose()
    {
        _watcher?.Stop();
        _watcher?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
