using Microsoft.Extensions.Logging;
using System.IO;
using System.Xml.Linq;
using System.Linq;

public class EventosProcessor : IXmlProcessor
{
    public bool CanProcess(XDocument doc)
    {
        return doc.Descendants().Any(x => x.Name.LocalName == "evento");
    }

    public void Process(XDocument doc, string caminho, string destinoBase, string erro, ILogger logger)
    {
        try
        {
            string? tipo = ExtrairTipoEvento(doc);
            string? ano = ExtrairAnoEmissao(doc);
            string? mes = ExtrairMesEmissao(doc);

            if (string.IsNullOrEmpty(tipo) || string.IsNullOrEmpty(ano) || string.IsNullOrEmpty(mes))
            {
                var filtros = new Dictionary<string, string>
                {
                    { "Tipo", tipo ?? "" },
                    { "Ano", ano ?? "" },
                    { "Mes", mes ?? "" }
                };
                FileHelpers.MoverParaErro(caminho, erro, logger, filtros);
                return;
            }

            // checagem duplicidade por infEvento.Id
            string? chave = ExtrairChave(doc);
            if (string.IsNullOrEmpty(chave))
            {
                var filtros = new Dictionary<string, string>
                {
                    { "Tipo", tipo ?? "" },
                    { "Ano", ano ?? "" },
                    { "Mes", mes ?? "" },
                    { "Chave", "" }
                };
                FileHelpers.MoverParaErro(caminho, erro, logger, filtros);
                return;
            }

            if (DuplicateChecker.IsDuplicateAndRegister(chave, "EVENTO", destinoBase))
            {
                var filtros = new Dictionary<string, string>
                {
                    { "Motivo", "Duplicidade" },
                    { "Chave", chave }
                };
                FileHelpers.MoverParaErro(caminho, erro, logger, filtros);
                return;
            }

            string destino = Path.Combine(destinoBase, "Eventos", tipo, ano, mes);
            Directory.CreateDirectory(destino);

            string destinoFinal = Path.Combine(destino, Path.GetFileName(caminho));
            File.Move(caminho, destinoFinal);

            logger.LogInformation("EVENTO {arquivo} MOVIDO PARA {destino}", caminho, destino);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ERRO AO PROCESSAR EVENTO {arquivo}", caminho);
            FileHelpers.MoverParaErro(caminho, erro, logger);
        }
    }

    private string? ExtrairTipoEvento(XDocument doc)
    {
        return doc.Descendants()
                  .Where(x => x.Name.LocalName == "detEvento")
                  .Descendants()
                  .FirstOrDefault(x => x.Name.LocalName == "descEvento")
                  ?.Value.Trim();
    }

    private string? ExtrairAnoEmissao(XDocument doc)
    {
        var dhEvento = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dhEvento")?.Value;
        if (!string.IsNullOrEmpty(dhEvento) && dhEvento.Length >= 4)
            return dhEvento.Substring(0, 4);
        return null;
    }

    private string? ExtrairMesEmissao(XDocument doc)
    {
        var dhEvento = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dhEvento")?.Value;
        if (!string.IsNullOrEmpty(dhEvento) && dhEvento.Length >= 7)
            return dhEvento.Substring(5, 2);
        return null;
    }

    private string? ExtrairChave(XDocument doc)
    {
        return doc.Descendants()
                  .FirstOrDefault(x => x.Name.LocalName == "infEvento")
                  ?.Attribute("Id")?.Value.Trim();
    }

}
