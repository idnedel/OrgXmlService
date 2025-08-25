using Microsoft.Extensions.Logging;
using System.IO;
using System.Xml.Linq;

public class NfseProcessor : IXmlProcessor
{
    public bool CanProcess(XDocument doc)
    {
        return doc.Descendants().Any(x => x.Name.LocalName == "Nfse");
    }

    public void Process(XDocument doc, string caminho, string destinoBase, string erro, ILogger logger)
    {
        try
        {
            string? cnpj = ExtrairCnpj(doc);
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

            string destino = Path.Combine(destinoBase, "NFSE", cnpj, ano, mes);
            Directory.CreateDirectory(destino);

            string destinoFinal = Path.Combine(destino, Path.GetFileName(caminho));
            File.Move(caminho, destinoFinal);

            logger.LogInformation("NFSE {arquivo} MOVIDA PARA {destino}", caminho, destino);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ERRO AO PROCESSAR NFSE {arquivo}", caminho);
            FileHelpers.MoverParaErro(caminho, erro, logger);
        }
    }

    private string? ExtrairCnpj(XDocument doc)
    {
        // busca padrão 1
        var cnpj1 = doc.Descendants()
            .Where(x => x.Name.LocalName == "TomadorServico")
            .Descendants()
            .Where(x => x.Name.LocalName == "IdentificacaoTomador")
            .Descendants()
            .Where(x => x.Name.LocalName == "CpfCnpj")
            .Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "Cnpj")
            ?.Value.Trim();

        if (!string.IsNullOrEmpty(cnpj1))
            return cnpj1;

        // busca padrão 2
        var cnpj2 = doc.Descendants()
            .Where(x => x.Name.LocalName == "Tomador")
            .Descendants()
            .Where(x => x.Name.LocalName == "IdentificacaoTomador")
            .Descendants()
            .Where(x => x.Name.LocalName == "CpfCnpj")
            .Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "Cnpj")
            ?.Value.Trim();

        return cnpj2;
    }

    private string? ExtrairAnoEmissao(XDocument doc)
    {
        var DataEmissao = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "DataEmissao")?.Value;
        if (!string.IsNullOrEmpty(DataEmissao) && DataEmissao.Length >= 4)
            return DataEmissao.Substring(0, 4);
        return null;
    }

    private string? ExtrairMesEmissao(XDocument doc)
    {
        var DataEmissao = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "DataEmissao")?.Value;
        if (!string.IsNullOrEmpty(DataEmissao) && DataEmissao.Length >= 7)
            return DataEmissao.Substring(5, 2);
        return null;
    }
}
