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
    public class Mix2Extension : MarkupExtension
    {
        public string Base { get; }
        public string Adding { get; }
        public float Balance { get; }
        public Type TargetType { set; get; }

        public Mix2Extension(string @base, string adding, float balance)
        {
            this.Base = @base;
            this.Adding = adding;
            this.Balance = balance;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            Binding MakeBinding(string path)
                => new Binding(path) { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = TargetType } };

            var binding = new MultiBinding();
            binding.Bindings.Add(MakeBinding(Base));
            binding.Bindings.Add(MakeBinding(Adding));
            binding.Converter = new Mix2Converter(Balance);

            return binding;
        }

        class Mix2Converter : IMultiValueConverter
        {
            readonly float Balance;

            public Mix2Converter(float balance)
            {
                Balance = balance;
            }

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                Color baseColor = values[0] is SolidColorBrush bBrsh ? bBrsh.Color : Colors.White;
                Color adngColor = values[1] is SolidColorBrush fBrsh ? fBrsh.Color : Colors.Black;

                byte BalanceColor(byte @base, byte adding, float balance)
                    => (byte)(@base + (adding - @base) * balance);

                var newColor = Color.FromArgb(
                    255,
                    BalanceColor(baseColor.R, adngColor.R, Balance),
                    BalanceColor(baseColor.G, adngColor.G, Balance),
                    BalanceColor(baseColor.B, adngColor.B, Balance));

                return new SolidColorBrush(newColor);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
