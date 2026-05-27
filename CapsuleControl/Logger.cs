using System;
using System.IO;

namespace CapsuleControl;

public static class Logger
{
    private static readonly string LogDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "logs");

    private static string LogFile =>
        Path.Combine(LogDir, $"capsule_{DateTime.Now:yyyy-MM-dd}.log");

    static Logger()
    {
        try { Directory.CreateDirectory(LogDir); } catch { }
    }

    public static void Info(string message)  => Write("INFO ", message);
    public static void Warn(string message)  => Write("WARN ", message);
    public static void Error(string message, Exception? ex = null)
    {
        Write("ERROR", ex == null ? message : $"{message} — {ex.Message}");
    }

    private static void Write(string level, string message)
    {
        try
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
            File.AppendAllText(LogFile, line + Environment.NewLine);
        }
        catch { }
    }
}
