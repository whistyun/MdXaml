using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Windows.Documents;
using MdXaml.Html.Core.Utils;
using System.Windows;
using System.Linq;

namespace MdXaml.Html.Core.Parsers
{
    public class OrderListParser : IBlockTagParser
    {
        public IEnumerable<string> SupportTag => new[] { "ol" };

        bool ITagParser.TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<TextElement> generated)
        {
            var rtn = TryReplace(node, manager, out var list);
            generated = list;
            return rtn;
        }

        public bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Block> generated)
        {
            var list = new List()
            {
                MarkerStyle = TextMarkerStyle.Decimal
            };

            var startAttr = node.Attributes["start"];
            if (startAttr is not null && Int32.TryParse(startAttr.Value, out var start))
            {
                list.StartIndex = start;
            }

            foreach (var listItemTag in node.ChildNodes.CollectTag("li"))
            {
                var itemContent = manager.ParseChildrenAndGroup(listItemTag);

                var listItem = new ListItem();
                listItem.Blocks.AddRange(itemContent);

                list.ListItems.Add(listItem);
            }

            generated = new[] { list };
            return true;
        }
    }
}
