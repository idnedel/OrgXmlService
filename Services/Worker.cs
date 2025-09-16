using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly LogPipeServer _pipeServer;
    private readonly FileDispatcher _dispatcher;
    private readonly string origem = @"C:\XML\pasta_origem_xml";   // pasta monitorada
    private readonly string destinoBase = @"C:\XML\pasta_destino_xml"; // base destino
    private readonly string erro = @"C:\XML\pasta_erros_xml"; // pasta para arquivos duplicados ou com erro

    private readonly ConcurrentQueue<string> filaArquivos = new();
    private Task? tarefaProcessamento;
    private FileSystemWatcher? watcher;

    public Worker(ILogger<Worker> logger, FileDispatcher dispatcher, LogPipeServer pipeServer)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _pipeServer = pipeServer; // implementação do servidor de pipe (validar)
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker: Pipe conectado? {status}", _pipeServer.IsClientConnected);
        _logger.LogInformation("Serviço de XML iniciado em: {time}", DateTimeOffset.Now);

        watcher = new FileSystemWatcher(origem, "*.xml")
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
        };

        watcher.Created += (sender, e) => filaArquivos.Enqueue(e.FullPath);

        foreach (var file in new DirectoryInfo(origem).GetFiles("*.xml", SearchOption.AllDirectories))
        {
            filaArquivos.Enqueue(file.FullName);
        }

        tarefaProcessamento = Task.Run(() => ProcessarArquivos(stoppingToken), stoppingToken);

        return Task.CompletedTask;
    }

    private void ProcessarArquivos(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (filaArquivos.TryDequeue(out var caminho))
            {
                try
                {
                    _dispatcher.Despachar(caminho, destinoBase, erro, _logger); 
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ERRO FATAL AO PROCESSAR ARQUIVO {arquivo}. Serviço continuará.", caminho);
                }
            }
            else
            {
                Thread.Sleep(200); // delay para evitar busy-wait
            }
        }
    }
}
