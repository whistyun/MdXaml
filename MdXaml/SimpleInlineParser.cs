using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Cache;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MdXaml.Plugins;

#if MIG_FREE
namespace Markdown.Xaml
#else
namespace MdXaml
#endif
{
    internal static class SimpleInlineParser
    {
        private sealed class Parser1 : IInlineParser
        {
            private readonly Func<Match, IEnumerable<Inline>> _converter;

            public Regex FirstMatchPattern { get; }

            public Parser1(Regex firstMatch, Func<Match, IEnumerable<Inline>> converter)
            {
                FirstMatchPattern = firstMatch;
                _converter = converter;
            }

            public IEnumerable<Inline> Parse(string text, Match firstMatch, IMarkdown engine, out int parseTextBegin, out int parseTextEnd)
            {
                parseTextBegin = firstMatch.Index;
                parseTextEnd = firstMatch.Index + firstMatch.Length;
                return _converter(firstMatch);
            }
        }

        private sealed class Parser2 : IInlineParser
        {
            private readonly Func<Match, Inline> _converter;

            public Regex FirstMatchPattern { get; }

            public Parser2(Regex firstMatch, Func<Match, Inline> converter)
            {
                FirstMatchPattern = firstMatch;
                _converter = converter;
            }

            public IEnumerable<Inline> Parse(string text, Match firstMatch, IMarkdown engine, out int parseTextBegin, out int parseTextEnd)
            {
                parseTextBegin = firstMatch.Index;
                parseTextEnd = firstMatch.Index + firstMatch.Length;
                return new[] { _converter(firstMatch) };
            }
        }

        public static IInlineParser New(Regex pattern, Func<Match, IEnumerable<Inline>> converter)
            => new Parser1(pattern, converter);

        public static IInlineParser New(Regex pattern, Func<Match, Inline> converter)
            => new Parser2(pattern, converter);
    }
}
