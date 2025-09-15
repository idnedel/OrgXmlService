using Microsoft.Extensions.Logging;

public class PipeLoggerProvider : ILoggerProvider
{
    private readonly LogPipeServer _pipeServer;

    public PipeLoggerProvider(LogPipeServer pipeServer)
    {
        _pipeServer = pipeServer;
    }

    public ILogger CreateLogger(string categoryName) => new PipeLogger(categoryName, _pipeServer);

    public void Dispose() { }

    private sealed class PipeLogger : ILogger
    {
        private readonly string _category;
        private readonly LogPipeServer _pipeServer;

        public PipeLogger(string category, LogPipeServer pipeServer)
        {
            _category = category;
            _pipeServer = pipeServer;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel,
                                EventId eventId,
                                TState state,
                                Exception? exception,
                                Func<TState, Exception?, string> formatter)
        {
            if (!_pipeServer.IsClientConnected) return;
            var msg = formatter(state, exception);
            if (!string.IsNullOrWhiteSpace(_category))
                msg = $"[{_category}] {msg}";
            if (exception != null)
                msg += " | EX: " + exception.GetType().Name + " - " + exception.Message;
            _pipeServer.TryEnqueue(msg, logLevel);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}

public static class PipeLoggerBuilderExtensions
{
    public static ILoggingBuilder AddPipeLogger(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, PipeLoggerProvider>();
        return builder;
    }
}