using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

public static class DuplicateChecker
{
    private static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    public static bool DuplicadaERegistrada(string chave, string tipo, string basePath)
    {
        if (string.IsNullOrWhiteSpace(chave)) return false;

        var dir = Path.Combine(basePath ?? AppContext.BaseDirectory, "_ChavesResgitradas");
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