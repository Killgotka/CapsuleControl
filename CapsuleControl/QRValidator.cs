using System;
using System.Configuration;
using System.Text;

namespace CapsuleControl;

public class QRValidator
{
    private readonly string _salt =
        ConfigurationManager.AppSettings["Salt"] ?? "spief-2026";

    public SessionData? Validate(string qrText)
    {
        try
        {
            byte[] raw;
            try
            {
                string b64 = qrText.Replace('-', '+').Replace('_', '/');
                int rem = b64.Length % 4;
                if (rem > 0) b64 += new string('=', 4 - rem);
                raw = Convert.FromBase64String(b64);
            }
            catch (Exception ex)
            {
                Logger.Warn($"QR decode failed: {ex.Message}");
                return null;
            }

            var xored = new byte[raw.Length];
            for (int i = 0; i < raw.Length; i++)
                xored[i] = (byte)(raw[i] ^ (i * 7 + 13));

            string[] parts = Encoding.UTF8.GetString(xored).Split('|');
            if (parts.Length != 4)
            {
                Logger.Warn("QR invalid: wrong field count");
                return null;
            }

            if (parts[3] != _salt)
            {
                Logger.Warn("QR invalid: wrong salt");
                return null;
            }

            if (!int.TryParse(parts[0], out int capsule) ||
                !int.TryParse(parts[1], out int extension) ||
                !long.TryParse(parts[2], out long unixTs))
            {
                Logger.Warn("QR invalid: failed to parse fields");
                return null;
            }

            return new SessionData
            {
                Capsule     = capsule,
                StartTime   = DateTimeOffset.FromUnixTimeSeconds(unixTs).LocalDateTime,
                IsExtension = extension != 0,
            };
        }
        catch (Exception ex)
        {
            Logger.Error("QR validation exception", ex);
            return null;
        }
    }
}
