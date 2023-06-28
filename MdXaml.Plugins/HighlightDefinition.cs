using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MdXaml.Plugins
{
    public class Definition
    {
        public string? Alias { get; set; }
        public Uri? Resource { get; set; }
        public string? RealName { get; set; }
    }
}
