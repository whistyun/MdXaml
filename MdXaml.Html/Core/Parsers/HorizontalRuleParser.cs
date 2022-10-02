using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MdXaml.Html.Core.Parsers
{
    public class HorizontalRuleParser : IBlockTagParser
    {
        public IEnumerable<string> SupportTag => new[] { "hr" };

        bool ITagParser.TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<TextElement> generated)
        {
            var rtn = TryReplace(node, manager, out var list);
            generated = list;
            return rtn;
        }

        public bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Block> generated)
        {
            var sep = new Separator();
            var container = new BlockUIContainer(sep)
            {
                Tag = manager.GetTag(Tags.TagRuleSingle)
            };

            generated = new[] { container };
            return true;
        }
    }
}
