using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using QRCoder;

namespace QRGenerator;

public static class StyledQRRenderer
{
    private static readonly Color Blue   = Color.FromArgb(255, 75,  141, 255);
    private static readonly Color Purple = Color.FromArgb(255, 168, 85,  247);

    public static Bitmap Render(QRCodeData data, Image logo)
    {
        var matrix = data.ModuleMatrix;
        int size   = matrix.Count;
        const int mod    = 14;
        const int quiet  = 4;
        int totalSize = (size + quiet * 2) * mod;
        int offset    = quiet * mod;

        var bmp = new Bitmap(totalSize, totalSize, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode   = PixelOffsetMode.HighQuality;

        // background: rounded white card with subtle shadow
        DrawBackground(g, totalSize);

        using var grad = new LinearGradientBrush(
            new PointF(0, 0), new PointF(totalSize, totalSize), Blue, Purple);

        // regular modules (skip finder + alignment areas)
        float modR = mod * 0.32f;
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                if (!matrix[r][c]) continue;
                if (IsFinderArea(r, c, size)) continue;

                float x = offset + c * mod + 1.5f;
                float y = offset + r * mod + 1.5f;
                float w = mod - 3f;
                FillRoundRect(g, grad, x, y, w, w, modR);
            }
        }

        // three finder patterns
        DrawFinder(g, grad, offset,                   offset,                   mod);
        DrawFinder(g, grad, offset + (size - 7) * mod, offset,                   mod);
        DrawFinder(g, grad, offset,                   offset + (size - 7) * mod, mod);

        // logo
        if (logo != null)
            DrawLogo(g, logo, totalSize);

        return bmp;
    }

    private static void DrawBackground(Graphics g, int totalSize)
    {
        // soft shadow
        for (int i = 6; i >= 1; i--)
        {
            int alpha = 8 * i;
            using var shadowBrush = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0));
            FillRoundRect(g, shadowBrush, i, i, totalSize - i, totalSize - i, 28);
        }
        using var bg = new SolidBrush(Color.White);
        FillRoundRect(g, bg, 0, 0, totalSize, totalSize, 24);
    }

    private static void DrawFinder(Graphics g, LinearGradientBrush grad, float x, float y, int mod)
    {
        float outer = 7 * mod;
        float outerR = mod * 1.1f;

        // outer rounded square
        FillRoundRect(g, grad, x, y, outer, outer, outerR);

        // white separator (erase inner 5x5 area)
        using var whiteBrush = new SolidBrush(Color.White);
        FillRoundRect(g, whiteBrush, x + mod, y + mod, 5 * mod, 5 * mod, outerR * 0.55f);

        // inner dot
        FillRoundRect(g, grad, x + 2 * mod, y + 2 * mod, 3 * mod, 3 * mod, outerR * 0.55f);
    }

    private static void DrawLogo(Graphics g, Image logo, int totalSize)
    {
        int logoSize = totalSize / 5;
        int cx = totalSize / 2;
        int cy = totalSize / 2;

        // white circle background with slight gradient border
        int pad = logoSize / 5;
        int bgSize = logoSize + pad * 2;
        int bgX = cx - bgSize / 2;
        int bgY = cy - bgSize / 2;

        // soft drop shadow for logo bg
        for (int i = 4; i >= 1; i--)
        {
            using var sh = new SolidBrush(Color.FromArgb(12 * i, 100, 80, 200));
            g.FillEllipse(sh, bgX - i, bgY - i, bgSize + i * 2, bgSize + i * 2);
        }

        using var whiteBg = new SolidBrush(Color.White);
        g.FillEllipse(whiteBg, bgX, bgY, bgSize, bgSize);

        // thin gradient ring
        using var ring = new Pen(new LinearGradientBrush(
            new PointF(bgX, bgY), new PointF(bgX + bgSize, bgY + bgSize), Blue, Purple), 2.5f);
        g.DrawEllipse(ring, bgX, bgY, bgSize, bgSize);

        // draw logo
        int lx = cx - logoSize / 2;
        int ly = cy - logoSize / 2;
        g.DrawImage(logo, lx, ly, logoSize, logoSize);
    }

    private static bool IsFinderArea(int r, int c, int size) =>
        (r < 9 && c < 9) ||
        (r < 9 && c >= size - 8) ||
        (r >= size - 8 && c < 9);

    private static void FillRoundRect(Graphics g, Brush brush, float x, float y, float w, float h, float r)
    {
        r = Math.Min(r, Math.Min(w, h) / 2f);
        using var path = RoundRectPath(x, y, w, h, r);
        g.FillPath(brush, path);
    }

    private static GraphicsPath RoundRectPath(float x, float y, float w, float h, float r)
    {
        float d = r * 2;
        var path = new GraphicsPath();
        path.AddArc(x,         y,         d, d, 180, 90);
        path.AddArc(x + w - d, y,         d, d, 270, 90);
        path.AddArc(x + w - d, y + h - d, d, d,   0, 90);
        path.AddArc(x,         y + h - d, d, d,  90, 90);
        path.CloseFigure();
        return path;
    }
}
