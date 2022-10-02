using HtmlAgilityPack;
using MdXaml.Html.Core.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MdXaml.Html.Core.Parsers
{
    public class ImageParser : IInlineTagParser
    {
        public IEnumerable<string> SupportTag => new[] { "img", "image" };

        bool ITagParser.TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<TextElement> generated)
        {
            var rtn = TryReplace(node, manager, out var list);
            generated = list;
            return rtn;
        }

        public bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Inline> generated)
        {
            var link = node.Attributes["src"]?.Value;
            var alt = node.Attributes["alt"]?.Value;
            if (link is null)
            {
                generated = EnumerableExt.Empty<Inline>();
                return false;
            }
            var title = node.Attributes["title"]?.Value;

            var widthTxt = node.Attributes["width"]?.Value;
            var heightTxt = node.Attributes["height"]?.Value;

            var oncompletes = new List<Action<InlineUIContainer, Image, ImageSource>>();

            if (Length.TryParse(heightTxt, out var heightLen))
            {
                if (heightLen.Unit == Unit.Percentage)
                {
                    oncompletes.Add((container, image, source) =>
                    {
                        image.SetBinding(
                            Image.HeightProperty,
                            new Binding(nameof(Image.Width))
                            {
                                RelativeSource = new RelativeSource(RelativeSourceMode.Self),
                                Converter = new MultiplyConverter(heightLen.Value / 100)
                            });
                    });
                }
                else
                {
                    oncompletes.Add((container, image, source) =>
                    {
                        image.Height = heightLen.ToPoint();
                    });
                }
            }

            // Bind size so document is updated when image is downloaded
            if (Length.TryParse(widthTxt, out var widthLen))
            {
                if (widthLen.Unit == Unit.Percentage)
                {
                    oncompletes.Add((container, image, source) =>
                    {
                        var parent = image.Parent;

                        for (; ; )
                        {
                            if (parent is FrameworkElement element)
                            {
                                parent = element;
                                break;
                            }
                            else if (parent is FrameworkContentElement content)
                            {
                                parent = content.Parent;
                            }
                            else break;
                        }

                        if (parent is FlowDocumentScrollViewer)
                        {
                            var binding = CreateMultiBindingForFlowDocumentScrollViewer();
                            binding.Converter = new MultiMultiplyConverter2(widthLen.Value / 100);
                            image.SetBinding(Image.WidthProperty, binding);
                        }
                        else
                        {
                            var binding = CreateBinding(nameof(FrameworkElement.ActualWidth), typeof(FrameworkElement));
                            binding.Converter = new MultiplyConverter(widthLen.Value / 100);
                            image.SetBinding(Image.WidthProperty, binding);
                        }
                    });
                }
                else
                {
                    oncompletes.Add((container, image, source) =>
                    {
                        image.Width = widthLen.ToPoint();
                    });
                }
            }

            var container = manager.Engine.LoadImage(alt, link, title, Aggregate(oncompletes));

            generated = new[] { container };
            return true;
        }

        private MultiBinding CreateMultiBindingForFlowDocumentScrollViewer()
        {
            var binding = new MultiBinding();

            var totalWidth = CreateBinding(nameof(FlowDocumentScrollViewer.ActualWidth), typeof(FlowDocumentScrollViewer));
            var verticalBarVis = CreateBinding(nameof(FlowDocumentScrollViewer.VerticalScrollBarVisibility), typeof(FlowDocumentScrollViewer));

            binding.Bindings.Add(totalWidth);
            binding.Bindings.Add(verticalBarVis);

            return binding;
        }

        private static Binding CreateBinding(string propName, Type ancestorType)
        {
            return new Binding(propName)
            {
                RelativeSource = new RelativeSource()
                {
                    Mode = RelativeSourceMode.FindAncestor,
                    AncestorType = ancestorType,
                }
            };
        }

        private static Action<A, B, C>? Aggregate<A, B, C>(IEnumerable<Action<A, B, C>> actions)
        {
            var acts = actions.ToList();
            return acts.Count == 0 ?
                        null :
                        (a, b, c) => acts.ForEach(act => act(a, b, c));
        }

        class MultiplyConverter : IValueConverter
        {
            public double Value { get; }

            public MultiplyConverter(double v)
            {
                Value = v;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return Value * (Double)value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return ((Double)value) / Value;
            }
        }
        class MultiMultiplyConverter2 : IMultiValueConverter
        {
            public double Value { get; }

            public MultiMultiplyConverter2(double v)
            {
                Value = v;
            }

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                var value = (double)values[0];
                var visibility = (ScrollBarVisibility)values[1];

                if (visibility == ScrollBarVisibility.Visible)
                {
                    return Value * (value - SystemParameters.VerticalScrollBarWidth);
                }
                else
                {
                    return Value * (Double)value;
                }
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
