using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace AppFletesMueve.Services
{
    public static class LocalCrashLogger
    {
        private const string LogFileName = "mueve_crash_log.txt";

        public static async Task LogAsync(string message)
        {
            try
            {
                var folder = FileSystem.AppDataDirectory;
                var path = Path.Combine(folder, LogFileName);
                var entry = $"[{DateTime.UtcNow:O}] {message}\n";
                await File.AppendAllTextAsync(path, entry);
            }
            catch { }
        }
    }
}
