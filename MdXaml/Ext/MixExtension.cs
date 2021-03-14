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
    public class MixExtension : MarkupExtension
    {
        public string Base { get; }
        public Color Adding { get; }
        public float Balance { get; }
        public Type TargetType { set; get; }

        public MixExtension(string @base, Color adding, float balance)
        {
            this.Base = @base;
            this.Adding = adding;
            this.Balance = balance;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding(Base)
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = TargetType },
                Converter = new MixConverter(Adding, Balance)
            };
        }

        class MixConverter : IValueConverter
        {
            readonly Color Adding;
            readonly float Balance;

            public MixConverter(Color adding, float balance)
            {
                Adding = adding;
                Balance = balance;
            }


            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                Color baseColor;

                if (value is SolidColorBrush cBrush)
                {
                    baseColor = cBrush.Color;
                }
                else
                {
                    baseColor = Colors.Black;
                }

                byte BalanceColor(byte @base, byte adding, float balance)
                    => (byte)(@base + (adding - @base) * balance);

                var newColor = Color.FromArgb(
                    BalanceColor(baseColor.A, Adding.A, Balance),
                    BalanceColor(baseColor.R, Adding.R, Balance),
                    BalanceColor(baseColor.G, Adding.G, Balance),
                    BalanceColor(baseColor.B, Adding.B, Balance));

                return new SolidColorBrush(newColor);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
