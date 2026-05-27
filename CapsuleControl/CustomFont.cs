using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;

namespace CapsuleControl;

public static class FontManager
{
    private static readonly PrivateFontCollection _pfc = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded) return;
        string fontsDir = AppPaths.Fonts;
        if (!Directory.Exists(fontsDir))
        {
            Logger.Warn($"Fonts directory not found: {fontsDir}");
            return;
        }
        foreach (string file in Directory.GetFiles(fontsDir, "*.ttf"))
        {
            _pfc.AddFontFile(file);
            Logger.Info($"Font loaded: {Path.GetFileName(file)}");
        }
        _loaded = true;
    }

    public static Font Get(string familyName, float size, FontStyle style = FontStyle.Regular)
    {
        foreach (FontFamily family in _pfc.Families)
        {
            if (family.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase))
                return new Font(family, size, style);
        }
        // шрифт не найден в коллекции — fallback на системный
        Logger.Warn($"Font '{familyName}' not found in Fonts/, using Segoe UI");
        return new Font("Segoe UI", size, style);
    }
}
