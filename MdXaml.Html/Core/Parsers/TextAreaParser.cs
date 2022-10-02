using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MdXaml.Html.Core.Parsers
{
    public class TextAreaParser : IInlineTagParser
    {
        public IEnumerable<string> SupportTag => new[] { "textarea" };

        bool ITagParser.TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<TextElement> generated)
        {
            var rtn = TryReplace(node, manager, out var list);
            generated = list;
            return rtn;
        }

        public bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Inline> generated)
        {
            var area = new TextBox()
            {
                AcceptsReturn = true,
                AcceptsTab = true,
                Text = node.InnerText.Trim(),
                TextWrapping = TextWrapping.Wrap,
            };

            int? rows = null;
            int? cols = null;
            var rowsAttr = node.Attributes["rows"];
            var colsAttr = node.Attributes["cols"];

            if (rowsAttr is not null)
            {
                if (int.TryParse(rowsAttr.Value, out var v))
                    rows = v * 14;
            }
            if (colsAttr is not null)
            {
                if (int.TryParse(colsAttr.Value, out var v))
                    cols = v * 7;
            }

            if (rows.HasValue) area.Height = rows.Value;
            if (cols.HasValue) area.Width = cols.Value;

            generated = new[] { new InlineUIContainer(area) };
            return true;
        }
    }
}
