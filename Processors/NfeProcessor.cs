using Microsoft.Extensions.Logging;
using System.IO;
using System.Xml.Linq;

public class NfeProcessor : IXmlProcessor
{
    public bool CanProcess(XDocument doc)
    {
        return doc.Descendants().Any(x => x.Name.LocalName == "NFe");
    }

    public void Process(XDocument doc, string caminho, string destinoBase, string erro, ILogger logger)
    {
        try
        {
            string? cnpj = ExtrairCnpj(doc);
            cnpj = CnpjsHelper.NormalizarCnpj(cnpj ?? "");
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

            logger.LogInformation("CNPJ extraído: {cnpj}", cnpj);

            var cnpjsPermitidos = CnpjsHelper.ObterCnpjsPermitidos(@"M:\ORGXML\OrgXmlService\cnpjs.txt");   // caminho de teste
            //var cnpjsPermitidos = CnpjsHelper.ObterCnpjsPermitidos(@"C:\ORGXML\OrgXmlService\cnpjs.txt"); // caminho absoluto para o arquivo cnpjs.txt
            logger.LogInformation("CNPJs permitidos: {lista}", string.Join(", ", cnpjsPermitidos));

            string destino;

            if (cnpjsPermitidos.Contains(cnpj))
            {
                destino = Path.Combine(destinoBase, "NFE", cnpj, ano, mes);
            }
            else
            {
                destino = Path.Combine(destinoBase, "NFE", "OUTROS", cnpj, ano, mes);
            }

            Directory.CreateDirectory(destino);

            string destinoFinal = Path.Combine(destino, Path.GetFileName(caminho));
            File.Move(caminho, destinoFinal);

            logger.LogInformation("NFE {arquivo} MOVIDA PARA {destino}", caminho, destino);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ERRO AO PROCESSAR NFE {arquivo}", caminho);
            FileHelpers.MoverParaErro(caminho, erro, logger);
        }
    }

    private string? ExtrairCnpj(XDocument doc)
    {
        return doc.Descendants()
                  .Where(x => x.Name.LocalName == "dest")
                  .Descendants()
                  .FirstOrDefault(x => x.Name.LocalName == "CNPJ")
                  ?.Value.Trim();
    }

    private string? ExtrairAnoEmissao(XDocument doc)
    {
        var dhEmi = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dhEmi")?.Value;
        if (!string.IsNullOrEmpty(dhEmi) && dhEmi.Length >= 4)
            return dhEmi.Substring(0, 4);

        var dEmi = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dEmi")?.Value;
        if (!string.IsNullOrEmpty(dEmi) && dEmi.Length >= 4)
            return dEmi.Substring(0, 4);

        return null;
    }

    private string? ExtrairMesEmissao(XDocument doc)
    {
        var dhEmi = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dhEmi")?.Value;
        if (!string.IsNullOrEmpty(dhEmi) && dhEmi.Length >= 7)
            return dhEmi.Substring(5, 2);

        var dEmi = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dEmi")?.Value;
        if (!string.IsNullOrEmpty(dEmi) && dEmi.Length >= 7)
            return dEmi.Substring(5, 2);

        return null;
    }
}
