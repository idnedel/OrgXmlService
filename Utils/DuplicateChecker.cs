using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

public static class DuplicateChecker
{
    private static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    /// <summary>
    /// Verifica se a chave já existe para o tipo informado e, se não existir, registra-a.
    /// Retorna true se for duplicada (já estava registrada), false se foi registrada agora.
    /// </summary>
    public static bool IsDuplicateAndRegister(string chave, string tipo, string basePath)
    {
        if (string.IsNullOrWhiteSpace(chave)) return false;

        var dir = Path.Combine(basePath ?? AppContext.BaseDirectory, "_CHAVES-REGISTRADAS");
        Directory.CreateDirectory(dir);

        var file = Path.Combine(dir, $"{tipo}.txt");

        try
        {
            _lock.EnterUpgradeableReadLock();

            if (File.Exists(file))
            {
                foreach (var line in File.ReadLines(file))
                {
                    if (string.Equals(line?.Trim(), chave, StringComparison.Ordinal))
                        return true; // duplicada
                }
            }

            _lock.EnterWriteLock();
            // Append para garantir persistência mínima; usa nova linha
            File.AppendAllText(file, chave + Environment.NewLine);
            return false;
        }
        finally
        {
            if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            if (_lock.IsUpgradeableReadLockHeld) _lock.ExitUpgradeableReadLock();
        }
    }
}