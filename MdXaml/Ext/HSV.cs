using System;
using System.Windows.Media;

#if MIG_FREE
namespace Markdown.Xaml.Ext
#else
namespace MdXaml.Ext
#endif
{
    struct HSV
    {
        public int Hue;
        public byte Saturation;
        public byte Value;

        public HSV(Color color)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));
            int div = max - min;

            if (div == 0)
            {
                Hue = 0;
                Saturation = 0;
            }
            else
            {
                Hue =
                        (min == color.B) ? 60 * (color.G - color.R) / div + 60 :
                        (min == color.R) ? 60 * (color.B - color.G) / div + 180 :
                                           60 * (color.R - color.B) / div + 300;
                Saturation = (byte)div;
            }

            Value = (byte)max;
        }

        public Color ToColor()
        {
            if (Hue == 0 && Saturation == 0)
            {
                return Color.FromRgb(Value, Value, Value);
            }

            //byte c = Saturation;

            int HueInt = Hue / 60;

            int x = (int)(Saturation * (1 - Math.Abs((Hue / 60f) % 2 - 1)));

            Color FromRgb(int r, int g, int b)
                => Color.FromRgb((byte)r, (byte)g, (byte)b);


            switch (Hue / 60)
            {
                default:
                case 0: return FromRgb(Value, Value - Saturation + x, Value - Saturation);
                case 1: return FromRgb(Value - Saturation + x, Value, Value - Saturation);
                case 2: return FromRgb(Value - Saturation, Value, Value - Saturation + x);
                case 3: return FromRgb(Value - Saturation, Value - Saturation + x, Value);
                case 4: return FromRgb(Value - Saturation + x, Value - Saturation, Value);
                case 5:
                case 6: return FromRgb(Value, Value - Saturation, Value - Saturation + x);
            }
        }
    }
}
