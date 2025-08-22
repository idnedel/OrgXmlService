using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using System.Collections.Concurrent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private FileSystemWatcher? watcher;
    private readonly string origem = @"C:\XML\pasta_origem_xml";   // pasta monitorada
    private readonly string destinoBase = @"C:\XML\pasta_destino_xml"; // base destino
    private readonly string erro = @"C:\XML\pasta_erros_xml"; // pasta para arquivos duplicados ou com erro

    private readonly ConcurrentQueue<string> filaArquivos = new();
    private Task? tarefaProcessamento;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de XML iniciado em: {time}", DateTimeOffset.Now);

        watcher = new FileSystemWatcher(origem, "*.xml");
        watcher.Created += (s, e) => filaArquivos.Enqueue(e.FullPath);
        watcher.EnableRaisingEvents = true;

        foreach (var file in Directory.GetFiles(origem, "*.xml"))
        {
            filaArquivos.Enqueue(file);
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
                MoverArquivo(caminho);
            }
            else
            {
                Thread.Sleep(200); // delay para evitar busy-wait
            }
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        MoverArquivo(e.FullPath);
    }

    private void MoverArquivo(string caminho)
    {
        try
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    using (FileStream fs = File.Open(caminho, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                    break;
                }
                catch
                {
                    Thread.Sleep(500);
                }
            }

            string? cnpj = ExtrairCnpj(caminho);
            string? anoEmissao = ExtrairAnoEmissao(caminho);
            string? mesEmissao = ExtrairMesEmissao(caminho);

            // move para pasta de erro
            if (string.IsNullOrEmpty(cnpj) || string.IsNullOrEmpty(anoEmissao) || string.IsNullOrEmpty(mesEmissao))
            {
                if (!Directory.Exists(erro))
                    Directory.CreateDirectory(erro);

                string nomeArquivo = Path.GetFileName(caminho);
                string destinoErro = Path.Combine(erro, nomeArquivo);

                File.Move(caminho, destinoErro);
                _logger.LogWarning("ARQUIVO {arquivo} MOVIDO PARA PASTA DE ERRO POR FALTA DE DADOS", nomeArquivo);
                _logger.LogWarning("CNPJ: {cnpj}, Ano: {ano}, Mês: {mes}", cnpj, anoEmissao, mesEmissao);
                return;
            }

            string destino = Path.Combine(destinoBase, cnpj, anoEmissao, mesEmissao);

            if (!Directory.Exists(destino))
                Directory.CreateDirectory(destino);

            string nomeArquivoFinal = Path.GetFileName(caminho);
            string destinoFinal = Path.Combine(destino, nomeArquivoFinal);

            File.Move(caminho, destinoFinal);

            _logger.LogInformation("ARQUIVO {arquivo} MOVIDO PARA {destino}", nomeArquivoFinal, destino);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERRO AO MOVER ARQUIVO {arquivo} ", caminho);

            if (!Directory.Exists(erro))
                Directory.CreateDirectory(erro);

            File.Move(caminho, Path.Combine(erro, Path.GetFileName(caminho)));
        }
    }

    private string? ExtrairCnpj(string arquivo)
    {
        try
        {
            var doc = XDocument.Load(arquivo);

            // <dest><CNPJ>
            var cnpj = doc.Descendants()
                          .Where(x => x.Name.LocalName == "dest")
                          .Descendants()
                          .FirstOrDefault(x => x.Name.LocalName == "CNPJ")
                          ?.Value;

            if (string.IsNullOrEmpty(cnpj))
            {
                // log
                var tags = string.Join(", ", doc.Descendants().Select(x => x.Name.LocalName).Distinct());
                File.AppendAllText(@"C:\XML\debug_tags.txt",
                    $"{DateTime.Now} - Não achou CNPJ em {arquivo}. Tags: {tags}{Environment.NewLine}");
            }

            return cnpj?.Trim();
        }
        catch (Exception ex)
        {
            File.AppendAllText(@"C:\XML\log.txt",
                $"{DateTime.Now} - Erro ao extrair CNPJ de {arquivo}: {ex.Message}{Environment.NewLine}");
            return null;
        }
    }

    private string? ExtrairAnoEmissao(string arquivo)
    {
        try
        {
            var doc = XDocument.Load(arquivo);

            var dhEmi = doc.Descendants()
                       .FirstOrDefault(x => x.Name.LocalName == "dhEmi")
                       ?.Value;


            if (!string.IsNullOrEmpty(dhEmi) && dhEmi.Length >= 4)
            {
                // formato yyyy-MM-ddTHH:mm:ss
                return dhEmi.Substring(0, 4);
            }

            var dEmi = doc.Descendants()
           .FirstOrDefault(x => x.Name.LocalName == "dEmi")
           ?.Value;


            if (!string.IsNullOrEmpty(dEmi) && dEmi.Length >= 4)
            {
                // formato yyyy-MM-ddTHH:mm:ss
                return dEmi.Substring(0, 4);
            }

            else
            {
                File.AppendAllText(@"C:\XML\debug_tags.txt",
                    $"{DateTime.Now} - Não achou dhEmi ou dEmi em {arquivo}.{Environment.NewLine}");
                return null;
            }
        }
        catch (Exception ex)
        {
            File.AppendAllText(@"C:\XML\log.txt",
                $"{DateTime.Now} - Erro ao extrair ano de emissão de {arquivo}: {ex.Message}{Environment.NewLine}");
            return null;
        }
    }

    private string? ExtrairMesEmissao(string arquivo)
    {
        try
        {
            var doc = XDocument.Load(arquivo);

            var dhEmi = doc.Descendants()
                       .FirstOrDefault(x => x.Name.LocalName == "dhEmi")
                       ?.Value;

            if (!string.IsNullOrEmpty(dhEmi) && dhEmi.Length >= 7)
            {
                return dhEmi.Substring(5, 2);
            }

            var dEmi = doc.Descendants()
           .FirstOrDefault(x => x.Name.LocalName == "dEmi")
           ?.Value;

            if (!string.IsNullOrEmpty(dEmi) && dEmi.Length >= 7)
            {
                return dEmi.Substring(5, 2);
            }

            else
            {
                File.AppendAllText(@"C:\XML\debug_tags.txt",
                    $"{DateTime.Now} - Não achou dhEmi ou dEmi em {arquivo}.{Environment.NewLine}");
                return null;
            }
        }
        catch (Exception ex)
        {
            File.AppendAllText(@"C:\XML\log.txt",
                $"{DateTime.Now} - Erro ao extrair mês de emissão de {arquivo}: {ex.Message}{Environment.NewLine}");
            return null;
        }
    }


}
