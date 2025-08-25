using Microsoft.Extensions.Logging;
using System.IO;
using System.Xml.Linq;

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
            string destino = Path.Combine(destinoBase, "CTE");
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
}
