using BatteryNotificationService;

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
