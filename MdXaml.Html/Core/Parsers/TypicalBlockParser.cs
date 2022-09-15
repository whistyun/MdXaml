using HtmlAgilityPack;
using MdXaml.Html.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;

namespace MdXaml.Html.Core.Parsers
{
    public class TypicalBlockParser : IBlockTagParser
    {
        private const string _resource = "MdXaml.Html.Core.Parsers.TypicalBlockParser.tsv";
        private TypicalParseInfo _parser;

        public IEnumerable<string> SupportTag => new[] { _parser.HtmlTag };

        public TypicalBlockParser(TypicalParseInfo parser)
        {
            _parser = parser;
        }

        bool ITagParser.TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<TextElement> generated)
        {
            var rtn = _parser.TryReplace(node, manager, out var list);
            generated = list;
            return rtn;
        }

        public bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Block> generated)
        {
            var rtn = _parser.TryReplace(node, manager, out var list);
            generated = list.Cast<Block>();
            return rtn;
        }

        public static IEnumerable<TypicalBlockParser> Load()
        {
            foreach (var info in TypicalParseInfo.Load(_resource))
            {
                yield return new TypicalBlockParser(info);
            }
        }
    }
}
