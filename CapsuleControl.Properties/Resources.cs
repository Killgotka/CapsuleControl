using System;
using System.Drawing;
using System.IO;

namespace CapsuleControl.Properties;

internal static class Resources
{
    private static string ResPath(string name) =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", name + ".png");

    internal static Bitmap bg   => new Bitmap(ResPath("bg"));
    internal static Bitmap bg2  => new Bitmap(ResPath("bg2"));
    internal static Bitmap bg3  => new Bitmap(ResPath("bg3"));
    internal static Bitmap bg31 => new Bitmap(ResPath("bg31"));
}
