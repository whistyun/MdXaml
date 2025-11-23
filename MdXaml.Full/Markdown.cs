using MdXaml.AnimatedGif;
using MdXaml.Html;
using MdXaml.Plugins;
using MdXaml.Svg;
using System.Collections.Generic;
using System.Linq;

namespace MdXaml.Full
{
    public class Markdown : MdXaml.SyntaxHigh.Markdown
    {
        public override MdXamlPlugins? Plugins
        {
            get => base.Plugins;
            set
            {
                var nplg = value is null ? new MdXamlPlugins() : value;

                AddIfAbsent<HtmlPluginSetup>(nplg.Setups);
                AddIfAbsent<SvgPluginSetup>(nplg.Setups);
                AddIfAbsent<AnimatedGifPluginSetup>(nplg.Setups);

                base.Plugins = nplg;
            }
        }

        public Markdown()
        {
            Plugins = new MdXamlPlugins();
        }
    }
}
