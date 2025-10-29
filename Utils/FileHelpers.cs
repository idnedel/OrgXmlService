using Microsoft.Extensions.Logging;
using System.IO;
using System.Collections.Generic;

public static class FileHelpers
{
    public static void MoverParaErro(
        string caminho,
        string erro,
        ILogger logger,
        Dictionary<string, string>? filtros = null)
    {
        Directory.CreateDirectory(erro);
        string destino = Path.Combine(erro, Path.GetFileName(caminho));

        if (File.Exists(destino))
        {
            string novoNome = Path.GetFileNameWithoutExtension(caminho) + "_DUPLICADO" + ".xml";
            destino = Path.Combine(erro, novoNome);
        }

        if (filtros != null && filtros.ContainsKey("Motivo") && filtros["Motivo"] == "Duplicidade")
        {
            string novoNome = Path.GetFileNameWithoutExtension(caminho) + "_DUPLICADO" + ".xml";
            destino = Path.Combine(erro, novoNome);
        }

        File.Move(caminho, destino);

        // string de filtros para log
        string filtrosLog = filtros != null
            ? string.Join(", ", filtros.Select(kv => $"{kv.Key}={kv.Value}"))
            : "FILTROS FALTANDO";

        logger.LogWarning("ARQUIVO {arquivo} MOVIDO PARA ERRO. FILTROS: {filtros}", caminho, filtrosLog);
    }
}
