using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32.TaskScheduler;
using Serilog;

internal class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day) // log separado por dia
            .CreateLogger();

        CreateHostBuilder(args).Build().Run();
    }


    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .UseSerilog()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<LogPipeServer>();                    // (1)
                services.AddHostedService<LogPipeHostedService>();         // (2)
                services.AddLogging(b => b.AddPipeLogger());               // (3)
                services.AddHostedService<Worker>();
                services.AddSingleton<FileDispatcher>();
            });
}
