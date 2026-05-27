using System;
using System.Text;

namespace QRGenerator;

public static class QREncoder
{
    private const string Salt = "spief-2026";

    public static string Encode(int capsuleId, bool isExtension, DateTime startTime)
    {
        long unixTs = new DateTimeOffset(startTime).ToUnixTimeSeconds();
        string payload = $"{capsuleId}|{(isExtension ? 1 : 0)}|{unixTs}|{Salt}";
        byte[] raw = Encoding.UTF8.GetBytes(payload);
        byte[] xored = new byte[raw.Length];
        for (int i = 0; i < raw.Length; i++)
            xored[i] = (byte)(raw[i] ^ (i * 7 + 13));
        string b64 = Convert.ToBase64String(xored);
        return b64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
