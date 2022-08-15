using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

#if MIG_FREE
namespace Markdown.Xaml.Ext
#else
namespace MdXaml.Ext
#endif
{
    class BrightnessExtension : MarkupExtension
    {
        public Color Base { get; }
        public string Foreground { get; }
        public Type? TargetType { set; get; }

        public BrightnessExtension(Color @base, string foreground)
        {
            this.Base = @base;
            this.Foreground = foreground;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding(Foreground)
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = TargetType },
                Converter = new BrightnessConverter(Base)
            };
        }

        class BrightnessConverter : IValueConverter
        {
            readonly Color Base;

            public BrightnessConverter(Color @base)
            {
                Base = @base;
            }


            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                Color foregroundColor;

                if (value is SolidColorBrush cBrush)
                {
                    foregroundColor = cBrush.Color;
                }
                else
                {
                    foregroundColor = Colors.Black;
                }

                var newColor = Base.Brightness(foregroundColor);
                return new SolidColorBrush(newColor);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
