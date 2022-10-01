using MdXaml.Html.Core;
using MdXaml.Plugins;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Linq;
using TextRange = MdXaml.Html.Core.TextRange;

namespace MdXaml.Html
{
    public class HtmlBlockParser : IBlockParser
    {
        private static readonly Regex s_emptyLine = new Regex("\n{2,}", RegexOptions.Compiled);
        private static readonly Regex s_headTagPattern = new(@"^<[\t ]*(?'tagname'[a-z][a-z0-9]*)(?'attributes'[ \t][^>]*|/)?>",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex s_tagPattern = new(@"<(?'close'/?)[\t ]*(?'tagname'[a-z][a-z0-9]*)(?'attributes'[ \t][^>]*|/)?>",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private ReplaceManager _replacer;

        public HtmlBlockParser()
        {
            _replacer = new ReplaceManager();
            FirstMatchPattern = s_headTagPattern;
        }

        public Regex FirstMatchPattern { get; }

        public IEnumerable<Block> Parse(string text, Match firstMatch, bool supportTextAlignment, Markdown engine, out int parseTextBegin, out int parseTextEnd)
        {
            parseTextBegin = firstMatch.Index;
            parseTextEnd = SimpleHtmlUtils.SearchTagRangeContinuous(text, firstMatch);

            _replacer.Engine = engine;

            var textchip = text.Substring(parseTextBegin, parseTextEnd - parseTextBegin);

            return _replacer.Parse(textchip);
        }
    }
}
