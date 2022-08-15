using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Text.RegularExpressions;

namespace MdXaml.Plugins
{
#if MIG_FREE
    internal 
#else
    public
#endif
   class SyntaxManager
    {
        public bool EnableNoteBlock { set; get; } = true;
        public bool EnableTableBlock { set; get; } = true;
        public bool EnableListMarkerExt { set; get; } = true;
    }
}
