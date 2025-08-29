//using System.Xml.Linq;

//public static class XmlHelpers
//{
//    public static string? IdentificarTipoDocumento(XDocument doc)
//    {
//        var root = doc.Root;
//        if (root == null) return null;
//        var localName = root.Name.LocalName.ToLower();

//        if (localName.Contains("nfeproc") || localName.Contains("nfe"))
//            return "NFE";
//        if (localName.Contains("cteproc") || localName.Contains("cte"))
//            return "CTE";
//        if (localName.Contains("mdfeproc") || localName.Contains("mdfe"))
//            return "MDFE";
//        return "DESCONHECIDO";
//    }
//}
