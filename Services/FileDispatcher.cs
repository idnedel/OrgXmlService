using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public class FileDispatcher
{
    private readonly List<IXmlProcessor> _processors;

    public FileDispatcher()
    {
        _processors = new List<IXmlProcessor>
        {
            new NfeProcessor(),
            new CteProcessor(),
            new EventosProcessor(),
            new NfseProcessor(),
            // new MdfeProcessor(), // validar
        };
    }

    public void Despachar(string caminho, string destinoBase, string erro, ILogger logger)
    {
        try
        {
            var doc = XDocument.Load(caminho);

            var processor = _processors.FirstOrDefault(p => p.CanProcess(doc));

            if (processor != null)
            {
                processor.Process(doc, caminho, destinoBase, erro, logger);
            }
            else
            {
                logger.LogWarning("NENHUM PROCESSADOR ENCONTRADO PARA {arquivo}", caminho);
                FileHelpers.MoverParaErro(caminho, erro, logger);
            }
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "ERRO AO DESPACHAR {arquivo}", caminho);
            FileHelpers.MoverParaErro(caminho, erro, logger);
        }
    }
}
