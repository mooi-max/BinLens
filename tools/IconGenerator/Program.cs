using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

const int size = 256;
var drawing = new DrawingVisual();
using (var context = drawing.RenderOpen())
{
    context.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(36, 87, 197)), null, new Rect(0, 0, size, size), 54, 54);
    var text = new FormattedText(
        ">_",
        CultureInfo.InvariantCulture,
        FlowDirection.LeftToRight,
        new Typeface(new FontFamily("Cascadia Mono"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
        104,
        Brushes.White,
        1.0);
    context.DrawText(text, new Point((size - text.Width) / 2, (size - text.Height) / 2 - 7));
}

var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
bitmap.Render(drawing);
var encoder = new PngBitmapEncoder();
encoder.Frames.Add(BitmapFrame.Create(bitmap));
using var imageStream = new MemoryStream();
encoder.Save(imageStream);
var imageBytes = imageStream.ToArray();

var output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "GtfobinsOffline", "Assets", "App.ico"));
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
using var file = File.Create(output);
using var writer = new BinaryWriter(file);
writer.Write((ushort)0);
writer.Write((ushort)1);
writer.Write((ushort)1);
writer.Write((byte)0);
writer.Write((byte)0);
writer.Write((byte)0);
writer.Write((byte)0);
writer.Write((ushort)1);
writer.Write((ushort)32);
writer.Write(imageBytes.Length);
writer.Write(22);
writer.Write(imageBytes);
