using HtmlAgilityPack;
using System.Collections.Generic;
using System.Windows.Documents;
using MdXaml.Html.Core.Utils;
using System.Windows;

namespace MdXaml.Html.Core.Parsers
{
    public class UnorderListParser : IBlockTagParser
    {
        public IEnumerable<string> SupportTag => new[] { "ul" };

        bool ITagParser.TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<TextElement> generated)
        {
            var rtn = TryReplace(node, manager, out var list);
            generated = list;
            return rtn;
        }

        public bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Block> generated)
        {
            var list = new List();
            list.MarkerStyle = TextMarkerStyle.Disc;

            foreach (var listItemTag in node.ChildNodes.CollectTag("li"))
            {
                var itemContent = manager.ParseAndGroup(listItemTag.ChildNodes);

                var listItem = new ListItem();
                listItem.Blocks.AddRange(itemContent);

                list.ListItems.Add(listItem);
            }

            generated = new[] { list };
            return true;
        }
    }
}
