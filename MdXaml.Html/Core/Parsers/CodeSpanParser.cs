using HtmlAgilityPack;
using MdXaml.Html.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;

namespace MdXaml.Html.Core.Parsers
{
    public class CodeSpanParser : IInlineTagParser
    {
        public IEnumerable<string> SupportTag => new[] { "code", "kbd", "var" };

        bool ITagParser.TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<TextElement> generated)
        {
            var rtn = TryReplace(node, manager, out var list);
            generated = list;
            return rtn;
        }

        public bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Inline> generated)
        {
            // inline code support only Plain
            if (node.ChildNodes.All(e => e is HtmlCommentNode or HtmlTextNode))
            {
                var span = new Run(node.InnerText.Replace('\n', ' '))
                {
                    Tag = manager.GetTag(Tags.TagCode),
                };

                generated = new[] { span };
                return true;
            }

            generated = EnumerableExt.Empty<Inline>();
            return false;
        }
    }
}
