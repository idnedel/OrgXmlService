using Microsoft.Extensions.Logging;
using System.Xml.Linq;

public interface IXmlProcessor
{
    bool CanProcess(XDocument doc);
    void Process(XDocument doc, string caminho, string destinoBase, string erro, ILogger logger);
}
