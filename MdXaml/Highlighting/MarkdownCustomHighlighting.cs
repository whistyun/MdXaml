using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Highlighting;

namespace MdXaml.Highlighting
{
    /// <summary>
    /// 自定义高亮语法支持
    /// </summary>
    public static class MarkdownCustomHighlighting
    {
        /// <summary>
        /// 接收语言类型，返回语法高亮定义
        /// </summary>
        /// <param name="langcode"></param>
        /// <returns></returns>
        public delegate IHighlightingDefinition? GetHighlightingFunc(string langcode);

        /// <summary>
        /// 用户定义的
        /// </summary>
        public static GetHighlightingFunc? HighlightingResolver { get; set; } = null;
    }
}
