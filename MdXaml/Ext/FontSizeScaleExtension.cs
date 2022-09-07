using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

#if MIG_FREE
namespace Markdown.Xaml.Ext
#else
namespace MdXaml.Ext
#endif
{
    class FontSizeScaleExtension : MarkupExtension
    {
        private readonly float power;

        public Type? TargetType { set; get; }

        public FontSizeScaleExtension(float power)
        {
            this.power = power;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding(nameof(Control.FontSize))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = TargetType },
                Converter = new FontSizeScaleConverter(power)
            };
        }

        class FontSizeScaleConverter : IValueConverter
        {
            readonly float Power;

            public FontSizeScaleConverter(float power)
            {
                this.Power = power;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return System.Convert.ToSingle(value) * Power;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
