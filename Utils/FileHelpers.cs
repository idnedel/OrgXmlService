using Microsoft.Extensions.Logging;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public static class FileHelpers
{
    public static void MoverParaErro(
        string caminho,
        string erro,
        ILogger logger,
        Dictionary<string, string>? filtros = null)
    {
        Directory.CreateDirectory(erro);
        string fileName = Path.GetFileName(caminho);
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(caminho);
        string extension = Path.GetExtension(caminho) ?? ".xml";
        string destino = Path.Combine(erro, fileName);

        bool isDuplicidade = filtros != null && filtros.TryGetValue("Motivo", out var motivo) && motivo == "Duplicidade";

        if (File.Exists(destino) || isDuplicidade)
        {
            destino = GetNextDuplicatePath(erro, fileNameWithoutExt, extension);
        }

        File.Move(caminho, destino);

        // string de filtros para log
        string filtrosLog = filtros != null
            ? string.Join(", ", filtros.Select(kv => $"{kv.Key}={kv.Value}"))
            : "FILTROS FALTANDO";

        logger.LogWarning("ARQUIVO {arquivo} MOVIDO PARA ERRO. FILTROS: {filtros}", caminho, filtrosLog);
    }

    private static string GetNextDuplicatePath(string erroDir, string baseName, string extension)
    {
        var pattern = $"{baseName}_DUPLICADO*{extension}";
        var existentes = Directory.Exists(erroDir)
            ? Directory.GetFiles(erroDir, pattern)
            : new string[0];

        int maxIndex = 0;
        foreach (var f in existentes)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(f);
            var suffix = nameWithoutExt.Substring(baseName.Length + "_DUPLICADO".Length);
            if (string.IsNullOrEmpty(suffix))
            {
                maxIndex = Math.Max(maxIndex, 1);
            }
            else if (int.TryParse(suffix, out var v))
            {
                maxIndex = Math.Max(maxIndex, v);
            }
        }

        int nextIndex = (existentes.Length > 0) ? maxIndex + 1 : 1;
        string nextName = $"{baseName}_DUPLICADO{nextIndex.ToString("D2")}{extension}";
        return Path.Combine(erroDir, nextName);
    }
}
