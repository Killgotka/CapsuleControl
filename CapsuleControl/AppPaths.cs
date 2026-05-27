using System;
using System.IO;

namespace CapsuleControl;

// При PublishSingleFile exe распаковывается во временную папку.
// AppDomain.BaseDirectory указывает туда, а не рядом с exe.
// Этот класс всегда возвращает реальную папку рядом с .exe файлом.
public static class AppPaths
{
    public static readonly string ExeDir =
        Path.GetDirectoryName(Environment.ProcessPath ?? AppDomain.CurrentDomain.BaseDirectory)
        ?? AppDomain.CurrentDomain.BaseDirectory;

    public static string Fonts     => Path.Combine(ExeDir, "Fonts");
    public static string Resources => Path.Combine(ExeDir, "Resources");
    public static string Logs      => Path.Combine(ExeDir, "logs");
    public static string Data      => Path.Combine(ExeDir, "data");
}
