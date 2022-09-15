using HtmlAgilityPack;
using MdXaml.Html.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;

namespace MdXaml.Html.Core.Parsers
{
    public class TextNodeParser : IInlineTagParser
    {
        public IEnumerable<string> SupportTag => new[] { HtmlNode.HtmlNodeTypeNameText };

        bool ITagParser.TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<TextElement> generated)
        {
            var rtn = TryReplace(node, manager, out var list);
            generated = list;
            return rtn;
        }

        public bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Inline> generated)
        {
            if (node is HtmlTextNode textNode)
            {
                generated = new[] { new Run(textNode.Text.Replace('\n', ' ')) };
                return true;
            }

            generated = EnumerableExt.Empty<Inline>();
            return false;
        }
    }
}
