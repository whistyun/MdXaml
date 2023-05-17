using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MdXaml.Html.Core.Parsers
{
    public class ButtonParser : IInlineTagParser
    {
        public IEnumerable<string> SupportTag => new[] { "button" };

        bool ITagParser.TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<TextElement> generated)
        {
            var rtn = TryReplace(node, manager, out var list);
            generated = list;
            return rtn;
        }

        public bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Inline> generated)
        {
            var doc = new FlowDocument() {
                PagePadding = new Thickness(0),
            };
            doc.Blocks.AddRange(manager.ParseChildrenAndGroup(node));

            var box = new FlowDocumentScrollViewer()
            {
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Document = doc,
            };

            box.Loaded += (s, e) =>
            {
                var desiredWidth = box.DesiredSize.Width;
                var desiredHeight = box.DesiredSize.Height;


                for (int i = 0; i < 10; ++i)
                {
                    desiredWidth /= 2;
                    var size = new Size(desiredWidth, double.PositiveInfinity);

                    box.Measure(size);

                    if (desiredHeight != box.DesiredSize.Height) break;

                    // Give up because it will not be wrapped back.
                    if (i == 9) return;
                }

                var preferedWidth = desiredWidth * 2;

                for (int i = 0; i < 10; ++i)
                {
                    var width = (desiredWidth + preferedWidth) / 2;

                    var size = new Size(width, double.PositiveInfinity);
                    box.Measure(size);

                    if (desiredHeight == box.DesiredSize.Height)
                    {
                        preferedWidth = width;
                    }
                    else
                    {
                        desiredWidth = width;
                    }
                }

                box.Width = preferedWidth;
            };


            var btn = new Button()
            {
                Content = box,
                IsEnabled = false,
            };

            generated = new[] { new InlineUIContainer(btn) };
            return true;
        }
    }
}
