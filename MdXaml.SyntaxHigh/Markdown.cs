using MdXaml.Plugins;
using System.Collections.Generic;
using System.Linq;

namespace MdXaml.SyntaxHigh
{
    public class Markdown : MdXaml.Markdown
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

        public Markdown()
        {
            Plugins = new MdXamlPlugins();
        }

        protected void AddIfAbsent<T>(IList<IPluginSetup> plugins) where T : IPluginSetup, new()
        {
            if (!plugins.Any(p => p is T))
            {
                plugins.Add(new T());
            }
        }
    }
}
