using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;

namespace MdXaml.Html.Core.Parsers
{
    public interface ITagParser
    {
        IEnumerable<string> SupportTag { get; }
        bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<TextElement> generated);
    }

    public interface IInlineTagParser : ITagParser
    {
        bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Inline> generated);
    }

    public interface IBlockTagParser : ITagParser
    {
        bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Block> generated);
    }

    public interface IHasPriority
    {
        int Priority { get; }
    }

    public static class HasPriority
    {
        public const int DefaultPriority = 10000;

        public static int GetPriority(this ITagParser parser)
            => parser is IHasPriority prop ? prop.Priority : HasPriority.DefaultPriority;
    }
}
