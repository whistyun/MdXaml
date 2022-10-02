using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Text.RegularExpressions;

namespace MdXaml.Plugins
{
    public class SyntaxManager
    {
        public bool EnableNoteBlock { set; get; } = true;
        public bool EnableTableBlock { set; get; } = true;
        public bool EnableListMarkerExt { set; get; } = true;
    }
}
