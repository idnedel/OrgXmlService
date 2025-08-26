using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class CnpjsHelper
{
    public static HashSet<string> ObterCnpjsPermitidos(string caminhoArquivo)
    {
        var cnpjsPermitidos = new HashSet<string>();
        if (File.Exists(caminhoArquivo))
        {
            var linhas = File.ReadAllLines(caminhoArquivo);
            foreach (var linha in linhas)
            {
                var cnpj = NormalizarCnpj(linha.Trim());
                if (!string.IsNullOrEmpty(cnpj))
                {
                    cnpjsPermitidos.Add(cnpj);
                }
            }
        }
        return cnpjsPermitidos;
    }

    public static string NormalizarCnpj(string cnpj)
    {
        return new string(cnpj.Where(char.IsDigit).ToArray());
    }
}