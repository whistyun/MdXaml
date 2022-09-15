using MdXaml.Html.Core;
using MdXaml.Plugins;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace MdXaml.Html
{
    public class HtmlBlockParser : IBlockParser
    {
        private ReplaceManager _replacer;

        public HtmlBlockParser()
        {
            _replacer = new ReplaceManager();
            FirstMatchPattern = SimpleHtmlUtils.CreateTagstartPattern(_replacer.BlockTags);
        }

        public Regex FirstMatchPattern { get; }

        public IEnumerable<Block> Parse(string text, Match firstMatch, bool supportTextAlignment, Markdown engine, out int parseTextBegin, out int parseTextEnd)
        {
            parseTextBegin = firstMatch.Index;
            parseTextEnd = SimpleHtmlUtils.SearchTagRange(text, firstMatch);

            _replacer.Engine = engine;

            return _replacer.ParseBlock(text.Substring(parseTextBegin, parseTextEnd - parseTextBegin));
        }
    }
}
