using BatteryNotificationService;

/// <summary>
/// Entry point for the Battery Notification Service.
/// Configures and starts the hosted service.
/// </summary>
IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(opts =>
    {
        opts.ServiceName = "Battery Notification Service";
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
