using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MdXaml.Html
{
    internal static class HtmlUtils
    {
        private static readonly HashSet<string> _emptyList = new HashSet<string>(new[] {
            "area", "base", "br", "col", "embed", "hr", "img", "input", "keygen", "link", "meta", "param", "source",
        });

        private static readonly HashSet<string> _omittableList = new HashSet<string>(new[] {
            "caption", "dd", "dt", "li", "optgroup", "option", "p", "rp", "rt", "td", "tfoot", "th", "thead", "tr"
        });

        private static Regex TagPattern = new Regex(@"<(?'close'/?)[\t ]*(?'tagname'[a-z]+)(?'attributes'[ \t][^>]*|/)?>",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static Regex CreateTagstartPattern(IEnumerable<string> tags)
        {
            var taglist = string.Join("|", tags);

            return new Regex(@$"<[\t ]*(?'tagname'{taglist})(?'attributes'[ \t][^>]*|/)?>",
                RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public static int SearchTagRange(string text, Match tagStartPatternMatch)
        {
            int searchStart = tagStartPatternMatch.Index + tagStartPatternMatch.Length;

            if (tagStartPatternMatch.Value.EndsWith("/>"))
            {
                return searchStart;
            }
            else
            {
                int end = SearchTagEnd(text, searchStart, tagStartPatternMatch.Groups["tagname"].Value);
                return end == -1 ? text.Length : end;
            }
        }

        public static int SearchTagEnd(string text, int start, string startTagName)
        {
            var tags = new Stack<string>();
            tags.Push(startTagName);

            for (; ; )
            {
                var mch = TagPattern.Match(text, start);
                if (!mch.Success) return -1;

                start = mch.Index + mch.Length;

                if (mch.Value.EndsWith("/>"))
                {
                    // empty tag
                    continue;
                }

                var tagName = mch.Groups["tagname"].Value.ToLower();

                if (_emptyList.Contains(tagName))
                {
                    continue;
                }
                else if (String.IsNullOrEmpty(mch.Groups["close"].Value))
                {
                    // start tag
                    tags.Push(mch.Groups["tagname"].Value);
                }
                else
                {
                    // close tag

                    while (tags.Count > 0)
                    {
                        var peekTag = tags.Peek();

                        tags.Pop();

                        if (peekTag == tagName) break;
                    }

                    if (tags.Count == 0)
                    {
                        return mch.Index + mch.Length;
                    }
                }
            }
        }
    }
}
