using HtmlAgilityPack;
using HtmlXaml.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace MdXaml.Html
{
    public class HtmlPlugin : IBlockParserPlugin, IRunParserPlugin
    {
        private static readonly Regex _tagStartPattern = new Regex(@"
                <
                ([a-z]+)       # $1 = tag name
                ([ \t][^>]*)?  # $2 = attribute
                >",
                RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);


        private static readonly Regex _anyTagPattern = new Regex(@"
                <
                (/)?           # $1 = end
                ([a-z]+)       # $2 = tag name
                ([ \t][^>]*)?  # $3 = attribute
                ",
                RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);


        private static readonly HashSet<string> _emptyList = new HashSet<string>(new[] {
            "area", "base", "br", "col", "embed", "hr", "img", "input", "keygen", "link", "meta", "param", "source",
        });

        private static readonly HashSet<string> _omittableList = new HashSet<string>(new[] {
            "caption", "dd", "dt", "li", "optgroup", "option", "p", "rp", "rt", "td", "tfoot", "th", "thead", "tr"
        });

        private ReplaceManager _replacer = new ReplaceManager();
        private HtmlDocument _document = new HtmlDocument();

        public IEnumerable<Block> Parse(string text, Func<string, IEnumerable<Block>> defaultHandler)
        {
            int prefixStartIndex = 0;
            int matchStartIndex = 0;

            for (; matchStartIndex < text.Length;)
            {
                var mch = _tagStartPattern.Match(text, matchStartIndex);
                if (!mch.Success) break;

                var tagName = mch.Groups[1].Value.ToLower();
                var idx = mch.Index + mch.Value.Length;

                // is not body tag?
                if (!_replacer.MaybeSupportBodyTag(mch.Groups[1].Value))
                {
                    matchStartIndex = idx;
                    continue;
                }

                // empty tag?
                if (mch.Value.EndsWith("/>"))
                {
                    if (_emptyList.Contains(tagName))
                    {
                        var prefix = text.Substring(prefixStartIndex, mch.Index - prefixStartIndex);
                        foreach (var inline in defaultHandler(prefix))
                            yield return inline;

                        foreach (var b in _replacer.ParseBlock(mch.Value))
                            yield return b;

                        prefixStartIndex = idx;
                    }

                    matchStartIndex = idx;
                    continue;
                }

                int endIndex = SearchEndBlock(text, mch.Index, tagName);
                if (endIndex != -1)
                {
                    var prefix = text.Substring(prefixStartIndex, mch.Index - prefixStartIndex);
                    foreach (var inline in defaultHandler(prefix))
                        yield return inline;

                    var htmlTxt = text.Substring(mch.Index, endIndex - mch.Index);
                    foreach (var b in _replacer.ParseBlock(htmlTxt))
                        yield return b;

                    prefixStartIndex = endIndex;
                    matchStartIndex = endIndex;
                }
                else
                {
                    matchStartIndex = idx;
                }
            }

            {
                var leaveText = text.Substring(prefixStartIndex, text.Length - prefixStartIndex);
                foreach (var inline in defaultHandler(leaveText))
                    yield return inline;
            }
        }

        public IEnumerable<Inline> Parse(string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            int prefixStartIndex = 0;
            int matchStartIndex = 0;

            for (; matchStartIndex < text.Length;)
            {
                var mch = _tagStartPattern.Match(text, matchStartIndex);
                if (!mch.Success) break;

                var tagName = mch.Groups[1].Value.ToLower();
                var idx = mch.Index + mch.Value.Length;

                // is not body tag?
                if (!_replacer.MaybeSupportBodyTag(mch.Groups[1].Value))
                {
                    matchStartIndex = idx;
                    continue;
                }

                // empty tag?
                if (mch.Value.EndsWith("/>"))
                {
                    if (_emptyList.Contains(tagName))
                    {
                        var prefix = text.Substring(prefixStartIndex, mch.Index - prefixStartIndex);
                        foreach (var inline in defaultHandler(prefix))
                            yield return inline;

                        foreach (var b in _replacer.ParseInline(mch.Value))
                            yield return b;

                        prefixStartIndex = idx;
                    }

                    matchStartIndex = idx;
                    continue;
                }

                int endIndex = SearchEndBlock(text, mch.Index, tagName);
                if (endIndex != -1)
                {
                    var prefix = text.Substring(prefixStartIndex, mch.Index - prefixStartIndex);
                    foreach (var inline in defaultHandler(prefix))
                        yield return inline;

                    var htmlTxt = text.Substring(mch.Index, endIndex - mch.Index);
                    foreach (var b in _replacer.ParseInline(htmlTxt))
                        yield return b;

                    prefixStartIndex = endIndex;
                    matchStartIndex = endIndex;
                }
                else
                {
                    matchStartIndex = idx;
                }
            }

            {
                var leaveText = text.Substring(prefixStartIndex, text.Length - prefixStartIndex);
                foreach (var inline in defaultHandler(leaveText))
                    yield return inline;
            }
        }

        private int SearchEndBlock(string text, int textIndex, string tagName)
        {
            _document.LoadHtml(text.Substring(textIndex));
            var node = _document.DocumentNode.ChildNodes;

            for (int i = 0; i < node.Count; ++i)
            {
                if (node[i] is HtmlTextNode txtNode)
                {
                    if (i == 0) return -1;
                    return txtNode.StreamPosition + textIndex;
                }
            }
            return text.Length;
        }
    }
}
