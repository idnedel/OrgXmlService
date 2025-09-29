using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

internal class Program
{
    public static void Main(string[] args)
    {

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var logsDir = Path.Combine(baseDir, "logs");
        Directory.CreateDirectory(logsDir);

        Serilog.Debugging.SelfLog.Enable(msg =>
        {
            try
            {
                File.AppendAllText(Path.Combine(logsDir, "serilog-selflog.txt"), msg);
            }
            catch { }
        });

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(logsDir, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                shared: true)
            .CreateLogger();

        Log.Information("Inicializando serviço OrgXmlService");

        try
        {
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Erro ao inicializar o serviço");
        }
        finally
        {
            Log.CloseAndFlush();
        }

    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .UseSerilog()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<Worker>();
                services.AddSingleton<FileDispatcher>();
            });
}
