using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LibShop.Services;

/// <summary>Генерация текстовой капчи в виде изображения с искажениями.</summary>
public static class CaptchaService
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 5;
    private const int ImageWidth = 150;
    private const int ImageHeight = 50;

    private static readonly Random Random = new();

    private static byte NextByte(int min, int max) => (byte)Random.Next(min, max);

    /// <summary>Генерирует случайный код капчи.</summary>
    public static string GenerateCode()
    {
        char[] chars = new char[CodeLength];
        for (int i = 0; i < CodeLength; i++)
            chars[i] = Chars[Random.Next(Chars.Length)];
        return new string(chars);
    }

    /// <summary>Рисует код капчи на изображении с шумовыми линиями.</summary>
    public static ImageSource RenderImage(string code)
    {
        var visual = new DrawingVisual();

        using (DrawingContext dc = visual.RenderOpen())
        {
            // Фон.
            dc.DrawRectangle(
                new LinearGradientBrush(
                    Color.FromRgb(236, 240, 245), Color.FromRgb(216, 224, 235),
                    45),
                null, new Rect(0, 0, ImageWidth, ImageHeight));

            // Шумовые линии поверх фона.
            for (int i = 0; i < 6; i++)
            {
                var pen = new Pen(
                    new SolidColorBrush(Color.FromRgb(
                        NextByte(80, 200),
                        NextByte(80, 200),
                        NextByte(80, 200))),
                    Random.Next(1, 3));
                dc.DrawLine(pen,
                    new Point(Random.Next(ImageWidth), Random.Next(ImageHeight)),
                    new Point(Random.Next(ImageWidth), Random.Next(ImageHeight)));
            }

            // Символы со случайным наклоном и смещением.
            var typeface = new Typeface(new FontFamily("Segoe UI"),
                FontStyles.Italic, FontWeights.Bold, FontStretches.Normal);

            double x = 10;
            foreach (char c in code)
            {
                var brush = new LinearGradientBrush(
                    Color.FromRgb(NextByte(10, 110), NextByte(10, 110), NextByte(10, 110)),
                    Color.FromRgb(NextByte(100, 220), NextByte(100, 220), NextByte(100, 220)),
                    90);

                var text = new FormattedText(c.ToString(), CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, typeface, Random.Next(22, 30),
                    brush, 1.0);

                dc.PushTransform(new RotateTransform(Random.Next(-25, 25), x + text.Width / 2,
                    ImageHeight / 2 + Random.Next(-6, 6)));
                dc.DrawText(text, new Point(x, Random.Next(6, 16)));
                dc.Pop();

                x += text.Width + Random.Next(2, 8);
            }
        }

        var bitmap = new RenderTargetBitmap(ImageWidth, ImageHeight, 96, 96,
            PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>Сравнивает введённый код с эталоном без учёта регистра.</summary>
    public static bool Verify(string? input, string expected) =>
        !string.IsNullOrWhiteSpace(input) &&
        string.Equals(input.Trim(), expected, StringComparison.OrdinalIgnoreCase);
}
