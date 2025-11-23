using MdXaml.Plugins;
using System.Collections.Generic;
using System.Linq;

namespace MdXaml.SyntaxHigh
{
    public class MarkdownScrollViewer : MdXaml.MarkdownScrollViewer
    {
        public override MdXamlPlugins? Plugins
        {
            get => base.Plugins;
            set
            {
                var nplg = value is null ? new MdXamlPlugins() : value;

                AddIfAbsent<SyntaxHighPluginSetup>(nplg.Setups);

                base.Plugins = nplg;
            }
        }

        public MarkdownScrollViewer()
        {
            Plugins = new MdXamlPlugins();
        }
    }
}
