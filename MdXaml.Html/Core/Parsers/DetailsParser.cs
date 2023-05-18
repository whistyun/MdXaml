using HtmlAgilityPack;
using MdXaml.Html.Core.Utils;
using MdXaml.Plugins;
using System.Collections.Generic;
using System.ComponentModel;
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

            var header = Create(manager.Engine, manager.ParseChildrenAndGroup(summary));

            var expander = new Expander()
            {
                Header = header,
                Content = Create(manager.Engine, manager.Grouping(manager.ParseChildrenJagigng(content))),
            };

            SetupCursor(header.Document);
            header.PreviewMouseDown += (s, e) => Viewer_PreviewMouseDown(expander, s, e);

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

        private static void Viewer_PreviewMouseDown(Expander parent, object sender, MouseButtonEventArgs e)
        {
            var doc = (FlowDocumentScrollViewer)sender;

            if (e.LeftButton == MouseButtonState.Pressed
             && ShouldBubblingToExpander(doc, e.GetPosition(doc)))
            {
                parent.SetCurrentValue(Expander.IsExpandedProperty, !parent.IsExpanded);
                e.Handled = true;
            }
        }

        private static bool ShouldBubblingToExpander(FlowDocumentScrollViewer doc, Point point)
        {
            object? elemObj = doc.InputHitTest(point);

            while (elemObj is not null)
            {
                if (object.ReferenceEquals(doc, elemObj))
                {
                    return true;
                }
                else if (elemObj is FrameworkElement element)
                {
                    if (element is Image)
                    {
                        elemObj = element.Parent;
                    }
                    else
                    {
                        break;
                    }
                }
                else if (elemObj is Hyperlink)
                {
                    break;
                }
                else if (elemObj is FrameworkContentElement contentElement)
                {
                    elemObj = contentElement.Parent;
                }
            }

            return false;
        }

        private static void SetupCursor(FlowDocument doc)
        {
            doc.Cursor = Cursors.Arrow;

            foreach (var elem in doc.Blocks)
                SetupCursor(elem);
        }

        private static void SetupCursor(Block block)
        {
            block.Cursor = Cursors.Arrow;

            if (block is List list)
            {
                foreach (var item in list.ListItems)
                    foreach (var elem in item.Blocks)
                        SetupCursor(elem);
            }
            else if (block is Paragraph paragraph)
            {
                foreach (var elem in paragraph.Inlines)
                    SetupCursor(elem);
            }
            else if (block is Section section)
            {
                foreach (var elem in section.Blocks)
                    SetupCursor(elem);
            }
            else if (block is Table table)
            {
                foreach (var rowGroup in table.RowGroups)
                    foreach (var row in rowGroup.Rows)
                        foreach (var cell in row.Cells)
                            foreach (var elem in cell.Blocks)
                                SetupCursor(elem);
            }
        }

        private static void SetupCursor(Inline element)
        {
            if (element is Hyperlink)
            {
                return;
            }
            else if (element is Span span)
            {
                foreach (var elem in span.Inlines)
                    SetupCursor(elem);
            }

            element.Cursor = Cursors.Arrow;
        }

        private static void Doc_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled)
                return;

            if (((FrameworkElement)sender).Parent is UIElement parent)
            {
                // bubling
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                parent.RaiseEvent(eventArg);

                e.Handled = true;
            }
        }
    }
}
