using Microsoft.Extensions.Hosting;

public class LogPipeHostedService : IHostedService
{
    private readonly LogPipeServer _server;

    public LogPipeHostedService(LogPipeServer server)
    {
        _server = server;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _server.Start();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _server.DisposeAsync();
    }
}