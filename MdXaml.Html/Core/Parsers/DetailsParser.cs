using HtmlAgilityPack;
using MdXaml.Html.Core.Utils;
using MdXaml.Plugins;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml.Linq;

namespace MdXaml.Html.Core.Parsers
{
    public class DetailsParser : IBlockTagParser
    {
        public IEnumerable<string> SupportTag => new[] { "details" };

        bool ITagParser.TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<TextElement> generated)
        {
            var rtn = TryReplace(node, manager, out var list);
            generated = list;
            return rtn;
        }

        public bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Block> generated)
        {
            var summary = node.ChildNodes.FirstOrDefault(e => e.IsElement("summary"));
            if (summary is null)
            {
                generated = EnumerableExt.Empty<Block>();
                return false;
            }

            var content = node.ChildNodes.Where(e => !ReferenceEquals(e, summary));

            var expander = new Expander()
            {
                Header = Create(manager.Engine, manager.ParseChildrenAndGroup(summary)),
                Content = Create(manager.Engine, manager.Grouping(manager.ParseChildrenJagigng(content))),
            };

            var container = new BlockUIContainer(expander);

            if (node.Attributes["open"] is HtmlAttribute openAttr
                && bool.TryParse(openAttr.Value, out var isOpened))
            {
                expander.IsExpanded = isOpened;
            }

            generated = new[] { container };
            return true;
        }

        private static FlowDocumentScrollViewer Create(IMarkdown engine, IEnumerable<Block> blocks)
        {
            var doc = new FlowDocument()
            {
                PagePadding = new Thickness(0),
            };
            doc.Blocks.AddRange(blocks);

            if (engine is Markdown)
            {
                doc.SetBinding(
                        FlowDocument.StyleProperty,
                        new Binding(Markdown.DocumentStyleProperty.Name)
                        {
                            Source = engine
                        });
            }


            var viewer = new FlowDocumentScrollViewer()
            {
                Document = doc,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };
            viewer.PreviewMouseWheel += Doc_PreviewMouseWheel;

            return viewer;
        }

        private static void Doc_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled)
                return;

            if (((FrameworkElement)sender).Parent is UIElement parent)
            {
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                parent.RaiseEvent(eventArg);

                e.Handled = true;
            }
        }
    }
}
