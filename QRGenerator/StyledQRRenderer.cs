using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using QRCoder;

namespace QRGenerator;

public static class StyledQRRenderer
{
    public static Bitmap Render(QRCodeData data, Image? logo)
    {
        var matrix = data.ModuleMatrix;
        int size      = matrix.Count;
        const int mod   = 14;
        const int quiet = 4;
        int totalSize = (size + quiet * 2) * mod;
        int offset    = quiet * mod;

        var bmp = new Bitmap(totalSize, totalSize, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode   = PixelOffsetMode.HighQuality;
        g.Clear(Color.White);

        // draw all modules as solid black squares — максимальный контраст для сканера
        using var black = new SolidBrush(Color.Black);
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                if (!matrix[r][c]) continue;
                g.FillRectangle(black, offset + c * mod, offset + r * mod, mod, mod);
            }
        }

        // лого поверх — с белым кругом-подложкой
        if (logo != null)
            DrawLogo(g, logo, totalSize);

        return bmp;
    }

    private static void DrawLogo(Graphics g, Image logo, int totalSize)
    {
        // лого занимает ~20% QR — при ECCLevel.H (30% коррекция) QR остаётся читаемым
        int logoSize = (int)(totalSize * 0.20);
        int cx = totalSize / 2;
        int cy = totalSize / 2;

        int pad   = logoSize / 4;
        int bgSize = logoSize + pad * 2;
        int bgX   = cx - bgSize / 2;
        int bgY   = cy - bgSize / 2;

        g.SmoothingMode = SmoothingMode.AntiAlias;

        // белый круг-подложка
        using var whiteBg = new SolidBrush(Color.White);
        g.FillEllipse(whiteBg, bgX, bgY, bgSize, bgSize);

        g.SmoothingMode = SmoothingMode.None;

        // лого
        int lx = cx - logoSize / 2;
        int ly = cy - logoSize / 2;
        g.DrawImage(logo, lx, ly, logoSize, logoSize);
    }
}
