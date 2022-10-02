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
    internal delegate Block InternalConverter(Match matchResult, bool supportTextAlignment);

    internal static class SimpleBlockParser
    {
        private sealed class Parser1 : IBlockParser
        {
            private readonly InternalConverter _converter;

            public Regex FirstMatchPattern { get; }

            public Parser1(Regex firstMatch, InternalConverter converter)
            {
                FirstMatchPattern = firstMatch;
                _converter = converter;
            }

            public IEnumerable<Block> Parse(string text, Match firstMatch, bool supportAlignment, IMarkdown engine, out int parseTextBegin, out int parseTextEnd)
            {
                parseTextBegin = firstMatch.Index;
                parseTextEnd = firstMatch.Index + firstMatch.Length;
                return new[] { _converter(firstMatch, supportAlignment) };
            }
        }

        private sealed class Parser2 : IBlockParser
        {
            private readonly Func<Match, IEnumerable<Block>> _converter;

            public Regex FirstMatchPattern { get; }

            public Parser2(Regex firstMatch, Func<Match, IEnumerable<Block>> converter)
            {
                FirstMatchPattern = firstMatch;
                _converter = converter;
            }

            public IEnumerable<Block> Parse(string text, Match firstMatch, bool supportAlignment, IMarkdown engine, out int parseTextBegin, out int parseTextEnd)
            {
                parseTextBegin = firstMatch.Index;
                parseTextEnd = firstMatch.Index + firstMatch.Length;
                return _converter(firstMatch);
            }
        }

        private sealed class Parser3 : IBlockParser
        {
            private readonly Func<Match, Block> _converter;

            public Regex FirstMatchPattern { get; }

            public Parser3(Regex firstMatch, Func<Match, Block> converter)
            {
                FirstMatchPattern = firstMatch;
                _converter = converter;
            }

            public IEnumerable<Block> Parse(string text, Match firstMatch, bool supportAlignment, IMarkdown engine, out int parseTextBegin, out int parseTextEnd)
            {
                parseTextBegin = firstMatch.Index;
                parseTextEnd = firstMatch.Index + firstMatch.Length;
                return new[] { _converter(firstMatch) };
            }
        }

        public static IBlockParser New(Regex pattern, InternalConverter converter)
            => new Parser1(pattern, converter);

        public static IBlockParser New(Regex pattern, Func<Match, IEnumerable<Block>> converter)
            => new Parser2(pattern, converter);

        public static IBlockParser New(Regex pattern, Func<Match, Block> converter)
            => new Parser3(pattern, converter);
    }

}
