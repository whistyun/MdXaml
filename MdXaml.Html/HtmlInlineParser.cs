using MdXaml.Html.Core;
using MdXaml.Plugins;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace MdXaml.Html
{
    public class HtmlInlineParser : IInlineParser
    {
        private readonly ReplaceManager _replacer;

        public HtmlInlineParser()
        {
            _replacer = new ReplaceManager();
            FirstMatchPattern = SimpleHtmlUtils.CreateTagstartPattern(_replacer.InlineTags);
        }

        public Regex FirstMatchPattern { get; }

        public IEnumerable<Inline> Parse(string text, Match firstMatch, IMarkdown engine, out int parseTextBegin, out int parseTextEnd)
        {
            parseTextBegin = firstMatch.Index;
            parseTextEnd = SimpleHtmlUtils.SearchTagRange(text, firstMatch);

            _replacer.Engine = engine;

            return _replacer.ParseInline(text.Substring(parseTextBegin, parseTextEnd - parseTextBegin));
        }
    }
}
