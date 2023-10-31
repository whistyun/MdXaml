using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Highlighting;

namespace MdXaml.Highlighting
{
    /// <summary>
    /// customized syntax highlighting
    /// </summary>
    public static class MarkdownCustomHighlighting
    {
        /// <summary>
        /// delegate for customize highlighting
        /// </summary>
        /// <param name="langcode">language code to highlight</param>
        /// <returns>IHighlightingDefinition for the langcode</returns>
        public delegate IHighlightingDefinition? GetHighlightingFunc(string langcode);

        /// <summary>
        /// customized highlighting method
        /// </summary>
        public static GetHighlightingFunc? HighlightingResolver { get; set; } = null;
    }
}
