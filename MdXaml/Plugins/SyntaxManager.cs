using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Text.RegularExpressions;

#if MIG_FREE
namespace Markdown.Xaml.Plugins
#else
namespace MdXaml.Plugins
#endif
{
    public class SyntaxManager
    {
        public bool EnableNoteBlock { set; get; } = true;
        public bool EnableTableBlock { set; get; } = true;
        public bool EnableListMarkerExt { set; get; } = true;
    }
}
