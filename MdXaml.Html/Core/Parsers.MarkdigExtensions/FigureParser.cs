using HtmlAgilityPack;
using MdXaml.Html.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;

namespace MdXaml.Html.Core.Parsers.MarkdigExtensions
{
    public class FigureParser : IBlockTagParser
    {
        public IEnumerable<string> SupportTag => new[] { "figure" };

        bool ITagParser.TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<TextElement> generated)
        {
            var rtn = TryReplace(node, manager, out var list);
            generated = list;
            return rtn;
        }

        public bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Block> generated)
        {
            var captionPair =
                node.ChildNodes
                    .SkipComment()
                    .Filter(nd => string.Equals(nd.Name, "figcaption", StringComparison.OrdinalIgnoreCase));

            var captionList = captionPair.Item1;
            var contentList = captionPair.Item2;

            var captionBlock = manager.Grouping(manager.ParseJagging(captionList.SelectMany(c => c.ChildNodes)));
            var contentBlock = manager.Grouping(manager.ParseJagging(contentList));

            var section = new Section();
            section.Tag = manager.GetTag(Tags.TagFigure);
            section.Blocks.AddRange(contentBlock);
            section.Blocks.AddRange(captionBlock);

            generated = new[] { section };
            return false;
        }
    }
}
