using Microsoft.Extensions.Logging;
using System.IO;
using System.Xml.Linq;
using System.Linq;

public class CteProcessor : IXmlProcessor
{  
    public bool CanProcess(XDocument doc)
    {
        return doc.Descendants().Any(x => x.Name.LocalName == "CTe");
    }

    public void Process(XDocument doc, string caminho, string destinoBase, string erro, ILogger logger)
    {
        try
        {

            string? cnpj = ExtrairCnpjToma(doc, logger);
            string? ano = ExtrairAnoEmissao(doc);
            string? mes = ExtrairMesEmissao(doc);

            if (string.IsNullOrEmpty(cnpj) || string.IsNullOrEmpty(ano) || string.IsNullOrEmpty(mes))
            {
                var filtros = new Dictionary<string, string>
                {
                    { "CNPJ", cnpj ?? "" },
                    { "Ano", ano ?? "" },
                    { "Mes", mes ?? "" }
                };
                FileHelpers.MoverParaErro(caminho, erro, logger, filtros);
                return;
            }

            // checar chave interna infCte
            string? chave = ExtrairChave(doc);
            if (string.IsNullOrEmpty(chave))
            {
                var filtros = new Dictionary<string, string>
                {
                    { "CNPJ", cnpj ?? "" },
                    { "Ano", ano ?? "" },
                    { "Mes", mes ?? "" },
                    { "Chave", "" }
                };
                FileHelpers.MoverParaErro(caminho, erro, logger, filtros);
                return;
            }

            if (DuplicateChecker.DuplicadaERegistrada(chave, "CTE", destinoBase))
            {
                var filtros = new Dictionary<string, string>
                {
                    { "Motivo", "Duplicidade" },
                    { "Chave", chave }
                };
                FileHelpers.MoverParaErro(caminho, erro, logger, filtros);
                return;
            }

            string destino = Path.Combine(destinoBase, "CTE", cnpj, ano, mes);
            Directory.CreateDirectory(destino);

            string destinoFinal = Path.Combine(destino, Path.GetFileName(caminho));
            File.Move(caminho, destinoFinal);

            logger.LogInformation("CTE {arquivo} MOVIDA PARA {destino}", caminho, destino);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ERRO AO PROCESSAR CTE {arquivo}", caminho);
            FileHelpers.MoverParaErro(caminho, erro, logger);
        }
    }

    private string? ExtrairCnpjToma(XDocument doc, ILogger logger)
    {
        // busca por tag toma03 ou 04
        var tagToma = doc.Descendants()
        .Where(x => x.Name.LocalName == "toma03" || 
                    x.Name.LocalName == "toma3"  ||
                    x.Name.LocalName == "toma04" ||
                    x.Name.LocalName == "toma4"  )

        .Descendants()
        .FirstOrDefault(x => x.Name.LocalName == "toma")
        ?.Value.Trim();

        // logger.LogInformation($"RETORNANDO TAG TOMA: {tagToma}");

        if (string.IsNullOrEmpty(tagToma))
            return null;

        {
            switch (tagToma)
            {
                case "0": //0: Remetente
                    return doc.Descendants()
                        .Where(x => x.Name.LocalName == "rem")
                        .Descendants()
                        .FirstOrDefault(x => x.Name.LocalName == "CNPJ")
                        ?.Value.Trim();

                case "1": //1: Expedidor
                    return doc.Descendants()
                        .Where(x => x.Name.LocalName == "exped")
                        .Descendants()
                        .FirstOrDefault(x => x.Name.LocalName == "CNPJ")
                        ?.Value.Trim();

                case "2": //2: Recebedor
                    return doc.Descendants()
                       .Where(x => x.Name.LocalName == "receb")
                       .Descendants()
                       .FirstOrDefault(x => x.Name.LocalName == "CNPJ")
                       ?.Value.Trim();

                case "3": //3: Destinatário
                    return doc.Descendants()
                       .Where(x => x.Name.LocalName == "dest")
                       .Descendants()
                       .FirstOrDefault(x => x.Name.LocalName == "CNPJ")
                       ?.Value.Trim();

                case "4": //4: Outros
                    return doc.Descendants()
                       .Where(x => x.Name.LocalName == "toma04" ||
                                   x.Name.LocalName == "toma4" ||
                                   x.Name.LocalName == "toma")
                       .Descendants()
                       .FirstOrDefault(x => x.Name.LocalName == "CNPJ")
                       ?.Value.Trim();

                default:
                    return null;
            }
        }
    }

    private string? ExtrairAnoEmissao(XDocument doc)
    {
        var dhEmi = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dhEmi")?.Value;
        if (!string.IsNullOrEmpty(dhEmi) && dhEmi.Length >= 4)
            return dhEmi.Substring(0, 4);

        var dEmi = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dEmi")?.Value;    //modelo antigo de xml
        if (!string.IsNullOrEmpty(dEmi) && dEmi.Length >= 4)
            return dEmi.Substring(0, 4);

        return null;
    }

    private string? ExtrairMesEmissao(XDocument doc)
    {
        var dhEmi = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dhEmi")?.Value;
        if (!string.IsNullOrEmpty(dhEmi) && dhEmi.Length >= 7)
            return dhEmi.Substring(5, 2);

        var dEmi = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dEmi")?.Value;    //modelo antigo de xml
        if (!string.IsNullOrEmpty(dEmi) && dEmi.Length >= 7)
            return dEmi.Substring(5, 2);

        return null;
    }

    private string? ExtrairChave(XDocument doc)
    {
        return doc.Descendants()
                  .FirstOrDefault(x => x.Name.LocalName == "infCte")
                  ?.Attribute("Id")?.Value.Trim();
    }
}
