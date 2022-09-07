using HtmlXaml.Core;
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
            FirstMatchPattern = HtmlUtils.CreateTagstartPattern(_replacer.BlockTags);
        }

        public Regex FirstMatchPattern { get; }

        public IEnumerable<Block> Parse(string text, Match firstMatch, bool supportTextAlignment, Markdown engine, out int parseTextBegin, out int parseTextEnd)
        {
            parseTextBegin = firstMatch.Index;
            parseTextEnd = HtmlUtils.SearchTagRange(text, firstMatch);

            _replacer.AssetPathRoot = engine.AssetPathRoot;
            _replacer.BaseUri = engine.BaseUri;
            _replacer.HyperlinkCommand = engine.HyperlinkCommand;

            return _replacer.ParseBlock(text.Substring(parseTextBegin, parseTextEnd - parseTextBegin));
        }
    }
}
