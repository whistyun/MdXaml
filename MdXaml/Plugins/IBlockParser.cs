using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Text.RegularExpressions;

#if MIG_FREE
using Engine = Markdown.Xaml.Markdown;
#else
using Engine = MdXaml.Markdown;
#endif


namespace MdXaml.Plugins
{
#if MIG_FREE
    internal 
#else
    public
#endif
    interface IBlockParser
    {
        /// <summary>
        /// The head pattern of parsing. It is good for performance that the pattern is match persable syntax.
        /// </summary>
        Regex FirstMatchPattern { get; }

        /// <summary>
        /// Process passed text. The range 0 to parseTextBegin and the range parseTextEnd to text-end are parsed by other parsers that contains this instance.
        /// </summary>
        /// <param name="text">A text be parsing. It may be not equals to the entire text be passed to `Markdown.Transform`</param>
        /// <param name="firstMatch">The result of <see cref="FirstMatchPattern"/>. The `Success` property returns true.</param>
        /// <param name="parseTextBegin">The first index of the parsed range(include)</param>
        /// <param name="parseTextEnd">The end index of the parsed range(exclude).</param>
        /// <returns>Parsed result, or null if unable to parsed.</returns>
        IEnumerable<Block> Parse(
            string text, Match firstMatch, bool supportTextAlignment,
            Engine engine,
            out int parseTextBegin, out int parseTextEnd);
    }
}
