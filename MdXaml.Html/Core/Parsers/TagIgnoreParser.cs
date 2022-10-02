using HtmlAgilityPack;
using MdXaml.Html.Core.Utils;
using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace MdXaml.Html.Core.Parsers
{
    public class TagIgnoreParser : IBlockTagParser, IInlineTagParser
    {
        public IEnumerable<string> SupportTag => new[] { "title", "meta", "link", "script", "style", "datalist" };

        bool ITagParser.TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<TextElement> generated)
        {
            generated = EnumerableExt.Empty<TextElement>();
            return true;
        }

        public bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Block> generated)
        {
            generated = EnumerableExt.Empty<Block>();
            return true;
        }

        public bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Inline> generated)
        {
            generated = EnumerableExt.Empty<Inline>();
            return true;
        }
    }
}
