using System;
using System.Text;

namespace CapsuleControl;

public class QRValidator
{
	private readonly string _salt = "spief-2026";

	public SessionData? Validate(string qrText)
	{
		try
		{
			byte[] array;
			try
			{
				string text = qrText.Replace('-', '+').Replace('_', '/');
				int num = text.Length % 4;
				if (num > 0)
				{
					text += new string('=', 4 - num);
				}
				array = Convert.FromBase64String(text);
			}
			catch
			{
				throw new Exception("Неверный формат строки");
			}
			byte[] array2 = new byte[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = (byte)(array[i] ^ (i * 7 + 13));
			}
			string[] array3 = Encoding.UTF8.GetString(array2).Split('|');
			if (array3.Length != 4)
			{
				throw new Exception("Неверные данные");
			}
			if (array3[3] != _salt)
			{
				throw new Exception("Неверная подпись");
			}
			if (!int.TryParse(array3[0], out var result))
			{
				throw new Exception("Неверное число");
			}
			if (!int.TryParse(array3[1], out var result2))
			{
				throw new Exception("Неверное число");
			}
			if (!long.TryParse(array3[2], out var result3))
			{
				throw new Exception("Неверная дата");
			}
			_ = DateTimeOffset.FromUnixTimeSeconds(result3).LocalDateTime;
			return new SessionData
			{
				Capsule = result,
				StartTime = DateTimeOffset.FromUnixTimeSeconds(result3).LocalDateTime,
				IsExtension = result2
			};
		}
		catch
		{
			return null;
		}
	}
}
