using BatteryNotificationService;
using BatteryNotificationService.Logging;

/// <summary>
/// Entry point for the Battery Notification application.
/// Configures and runs the worker as a background process in the user session.
/// </summary>
var logPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "BatteryNotificationService",
    "service.log"
);

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
        logging.AddProvider(new FileLoggerProvider(logPath));
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
