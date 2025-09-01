//using Microsoft.Extensions.Logging;
//using System.IO;
//using System.Xml.Linq;

//public class MdfeProcessor : IXmlProcessor
//{
//	public bool CanProcess(XDocument doc)
//	{
//		return doc.Descendants().Any(x => x.Name.LocalName == "mdfe");
//	}

//	public void Process(XDocument doc, string caminho, string destinoBase, string erro, ILogger logger)
//	{
//		try
//		{
//			string? cnpj = ExtrairCnpj(doc);
//			string? ano = ExtrairAnoEmissao(doc);
//			string? mes = ExtrairMesEmissao(doc);

//			if (string.IsNullOrEmpty(cnpj) || string.IsNullOrEmpty(ano) || string.IsNullOrEmpty(mes))
//			{
//				var filtros = new Dictionary<string, string>
//				{
//					{ "CNPJ", cnpj ?? "" },
//					{ "Ano", ano ?? "" },
//					{ "Mes", mes ?? "" }
//				};
//				FileHelpers.MoverParaErro(caminho, erro, logger, filtros);
//				return;
//			}

//			string destino = Path.Combine(destinoBase, "MDFE", cnpj, ano, mes);
//			Directory.CreateDirectory(destino);

//			string destinoFinal = Path.Combine(destino, Path.GetFileName(caminho));
//			File.Move(caminho, destinoFinal);

//			logger.LogInformation("MDFE {arquivo} MOVIDA PARA {destino}", caminho, destino);
//		}
//		catch (Exception ex)
//		{
//			logger.LogError(ex, "ERRO AO PROCESSAR MDFE {arquivo}", caminho);
//			FileHelpers.MoverParaErro(caminho, erro, logger);
//		}
//	}


//	//validar ainda
//	private string? ExtrairCnpj(XDocument doc)
//	{
//		return doc.Descendants()
//				  .Where(x => x.Name.LocalName == "dest")
//				  .Descendants()
//				  .FirstOrDefault(x => x.Name.LocalName == "CNPJ")
//				  ?.Value.Trim();
//	}

//	private string? ExtrairAnoEmissao(XDocument doc)
//	{
//		var dhEmi = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dhEmi")?.Value;
//		if (!string.IsNullOrEmpty(dhEmi) && dhEmi.Length >= 4)
//			return dhEmi.Substring(0, 4);

//		var dEmi = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dEmi")?.Value;
//		if (!string.IsNullOrEmpty(dEmi) && dEmi.Length >= 4)
//			return dEmi.Substring(0, 4);

//		return null;
//	}

//	private string? ExtrairMesEmissao(XDocument doc)
//	{
//		var dhEmi = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dhEmi")?.Value;
//		if (!string.IsNullOrEmpty(dhEmi) && dhEmi.Length >= 7)
//			return dhEmi.Substring(5, 2);

//		var dEmi = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dEmi")?.Value;
//		if (!string.IsNullOrEmpty(dEmi) && dEmi.Length >= 7)
//			return dEmi.Substring(5, 2);

//		return null;
//	}
//}
