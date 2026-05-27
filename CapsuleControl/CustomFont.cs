using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;

namespace CapsuleControl;

public class CustomFont : IDisposable
{
	private PrivateFontCollection? _privateFontCollection;

	public FontFamily? FontFamily { get; private set; }

	public CustomFont(string fontFilePath)
	{
		if (!File.Exists(fontFilePath))
		{
			throw new FileNotFoundException("Шрифт не найден: " + fontFilePath);
		}
		_privateFontCollection = new PrivateFontCollection();
		_privateFontCollection.AddFontFile(fontFilePath);
		if (_privateFontCollection.Families.Length != 0)
		{
			FontFamily = _privateFontCollection.Families[0];
			Console.WriteLine("✅ Шрифт успешно загружен: " + FontFamily.Name);
			return;
		}
		throw new Exception("Не удалось загрузить семейство шрифтов");
	}

	public Font GetFont(float size, FontStyle style = FontStyle.Regular)
	{
		if (FontFamily == null)
		{
			return new Font("Segoe UI", size, style);
		}
		return new Font(FontFamily, size, style);
	}

	public void Dispose()
	{
		_privateFontCollection?.Dispose();
	}
}
