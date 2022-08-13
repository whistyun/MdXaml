using HtmlXaml.Core;
using MdXaml.Plugins;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace MdXaml.Html
{
    public class HtmlInlineParser : IInlineParser
    {
        private ReplaceManager _replacer;

        public HtmlInlineParser()
        {
            _replacer = new ReplaceManager();
            FirstMatchPattern = HtmlUtils.CreateTagstartPattern(_replacer.InlineTags);
        }

        public Regex FirstMatchPattern { get; }

        public IEnumerable<Inline> Parse(string text, Match firstMatch, out int parseTextBegin, out int parseTextEnd)
        {
            parseTextBegin = firstMatch.Index;
            parseTextEnd = HtmlUtils.SearchTagRange(text, firstMatch);

            return _replacer.ParseInline(text.Substring(parseTextBegin, parseTextEnd - parseTextBegin));
        }
    }
}
