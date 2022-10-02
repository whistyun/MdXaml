using HtmlAgilityPack;
using MdXaml.Html.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;

namespace MdXaml.Html.Core.Parsers
{
    public class CodeBlockParser : IBlockTagParser
    {
        public IEnumerable<string> SupportTag => new[] { "pre" };

        bool ITagParser.TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<TextElement> generated)
        {
            var rtn = TryReplace(node, manager, out var list);
            generated = list;
            return rtn;
        }

        public bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Block> generated)
        {
            generated = EnumerableExt.Empty<Block>();

            var codeElements = node.ChildNodes.CollectTag("code");
            if (codeElements.Count != 0)
            {
                var rslt = new List<Block>();

                foreach (var codeElement in codeElements)
                {
                    // "language-**", "lang-**", "**" or "sourceCode **"
                    var classVal = codeElement.Attributes["class"]?.Value;

                    var langCode = ParseLangCode(classVal);
                    rslt.Add(DocUtils.CreateCodeBlock(langCode, codeElement.InnerText, manager));
                }

                generated = rslt;
                return rslt.Count > 0;
            }
            else if (node.ChildNodes.TryCastTextNode(out var textNodes))
            {
                var buff = new StringBuilder();
                foreach (var textNode in textNodes)
                    buff.Append(textNode.InnerText);

                generated = new[] { DocUtils.CreateCodeBlock(null, buff.ToString(), manager) };
                return true;
            }
            else return false;
        }

        private static string ParseLangCode(string? classVal)
        {
            if (classVal is null) return "";

            // "language-**", "lang-**", "**" or "sourceCode **"
            var indics = Enumerable.Range(0, classVal.Length)
                                   .Reverse()
                                   .Where(i => !Char.IsLetterOrDigit(classVal[i]));

            return classVal.Substring(indics.Any() ? indics.First() + 1 : 0);
        }
    }
}
