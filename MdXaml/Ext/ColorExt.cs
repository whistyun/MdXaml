using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

#if MIG_FREE
namespace Markdown.Xaml.Ext
#else
namespace MdXaml.Ext
#endif
{
    static class ColorExt
    {
        public static Color Brightness(this Color color, Color fore)
        {
            var foreMax = Math.Max(fore.R, Math.Max(fore.G, fore.B));
            var tgtHsv = new HSV(color);

            int newValue = tgtHsv.Value + foreMax;
            int newSaturation = tgtHsv.Saturation;
            if (newValue > 255)
            {
                var newSaturation2 = newSaturation - (newValue - 255);
                newValue = 255;

                var sChRtLm = (color.R >= color.G && color.R >= color.B) ? 0.95f * 0.7f :
                              (color.G >= color.R && color.G >= color.B) ? 0.95f :
                                                                           0.95f * 0.5f;

                var sChRt = Math.Max(sChRtLm, newSaturation2 / (float)newSaturation);
                if (Single.IsInfinity(sChRt)) sChRt = sChRtLm;

                newSaturation = (int)(newSaturation * sChRt);
            }

            tgtHsv.Value = (byte)newValue;
            tgtHsv.Saturation = (byte)newSaturation;

            var newColor = tgtHsv.ToColor();
            return newColor;
        }
    }
}
