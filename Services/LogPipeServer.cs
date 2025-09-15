using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

public sealed class LogPipeServer : IAsyncDisposable
{
    private const string PipeName = "OrgXmlLogPipe";
    private readonly CancellationTokenSource _cts = new();
    private readonly Channel<string> _queue;
    private readonly JsonSerializerOptions _jsonOptions;
    private Task? _acceptLoopTask;
    private Task? _writerLoopTask;
    private NamedPipeServerStream? _pipe;
    private readonly object _sync = new();
    private volatile bool _started;
    private volatile bool _stopping;
    private readonly ILogger _logger;

    public LogPipeServer(ILogger<LogPipeServer> logger, int queueCapacity = 5000)
    {
        _logger = logger;
        _queue = Channel.CreateBounded<string>(new BoundedChannelOptions(queueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest, // drop em logs antigos da fila
            SingleReader = true,
            SingleWriter = false
        });

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public void Start()
    {
        if (_started) return;
        _started = true;

        _acceptLoopTask = Task.Run(AcceptLoopAsync, _cts.Token);
        _writerLoopTask = Task.Run(WriterLoopAsync, _cts.Token);
        _logger.LogInformation("LogPipeServer iniciado (pipe: {pipe})", PipeName);
    }

    private async Task AcceptLoopAsync()
    {
        while (!_cts.IsCancellationRequested && !_stopping)
        {
            try
            {
                var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.Out,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                lock (_sync)
                {
                    _pipe = server;
                }

                _logger.LogInformation("Aguardando conexão no pipe {pipe}", PipeName);
                await server.WaitForConnectionAsync(_cts.Token);
                _logger.LogInformation("Cliente conectado ao pipe {pipe}", PipeName);

                // aguarda até desconectar ou cancelamento geral
                while (server.IsConnected && !_cts.IsCancellationRequested)
                {
                    await Task.Delay(500, _cts.Token);
                }

                _logger.LogInformation("Cliente desconectado do pipe {pipe}", PipeName);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no accept loop do pipe");
                await Task.Delay(2000, _cts.Token);
            }
            finally
            {
                lock (_sync)
                {
                    _pipe?.Dispose();
                    _pipe = null;
                }
            }
        }
    }

    private async Task WriterLoopAsync()
    {
        try
        {
            while (await _queue.Reader.WaitToReadAsync(_cts.Token))
            {
                while (_queue.Reader.TryRead(out var line))
                {
                    NamedPipeServerStream? pipeRef;
                    lock (_sync)
                    {
                        pipeRef = _pipe;
                    }

                    if (pipeRef == null || !pipeRef.IsConnected)
                        continue; // descarta

                    try
                    {
                        var bytes = Encoding.UTF8.GetBytes(line + "\n");
                        await pipeRef.WriteAsync(bytes, 0, bytes.Length, _cts.Token);
                        await pipeRef.FlushAsync(_cts.Token);
                    }
                    catch (IOException)
                    {
                        // desconexão do cliente
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Falha ao escrever log no pipe");
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    public bool IsClientConnected
    {
        get
        {
            lock (_sync)
            {
                return _pipe?.IsConnected == true;
            }
        }
    }

    public bool TryEnqueue(string message, LogLevel level)
    {
        if (!_started || _stopping) return false;

        var payload = new
        {
            timestamp = DateTimeOffset.UtcNow,
            level = level.ToString(),
            message,
            source = "OrgXmlService"
        };

        string json;
        try
        {
            json = JsonSerializer.Serialize(payload, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Falha ao serializar log para pipe");
            return false;
        }

        return _queue.Writer.TryWrite(json);
    }

    public async ValueTask DisposeAsync()
    {
        if (_stopping) return;
        _stopping = true;

        _cts.Cancel();
        _queue.Writer.TryComplete();

        var tasks = new[] { _acceptLoopTask, _writerLoopTask }
            .Where(t => t != null)
            .Cast<Task>()
            .ToArray();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch { /* ignorar cancelamento */ }

        lock (_sync)
        {
            _pipe?.Dispose();
            _pipe = null;
        }

        _cts.Dispose();
        _logger.LogInformation("LogPipeServer finalizado");
    }
}