using MdXaml.Plugins;
using Svg;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace MdXaml.Svg
{
    public class SvgImageLoader : IImageLoader
    {
        public BitmapImage? Load(Stream stream)
        {
            var doc = SvgDocument.Open<SvgDocument>(stream);
            var width = ToPoint(doc.Width);
            var height = ToPoint(doc.Height);

            using var img = new Bitmap((int)width, (int)height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(img))
            {
                doc.Draw(g, new SizeF(width, height));
            }

            var ms = new MemoryStream();
            img.Save(ms, ImageFormat.Png);

            ms.Position = 0;
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();

            return bi;
        }

        public static float ToPoint(SvgUnit length)
        {
            var value = length.Value;

            return length.Type switch
            {
                // TODO: binding to scrollviewer
                SvgUnitType.Percentage => 100,

                SvgUnitType.None
                or SvgUnitType.Pixel
                or SvgUnitType.User => value,
                SvgUnitType.Em => value * 11,
                SvgUnitType.Ex => value * 11 / 2,
                SvgUnitType.Inch => value * 96.0f,
                SvgUnitType.Centimeter => value * 37.7952755905512f,
                SvgUnitType.Millimeter => value * 3.77952755905512f,
                SvgUnitType.Pica => value * 1.33333333333333f,
                SvgUnitType.Point => value * 16,
                _ => throw new InvalidOperationException(),
            };
        }

    }
}
