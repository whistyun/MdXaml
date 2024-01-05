using MdXaml.AnimatedGif;
using MdXaml.Html;
using MdXaml.Plugins;
using MdXaml.Svg;
using Svg;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MdXaml.Full
{
    public class MarkdownScrollViewer : MdXaml.MarkdownScrollViewer
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

        public MarkdownScrollViewer()
        {
            Plugins = new MdXamlPlugins();
        }

        private void AddIfAbsent<T>(IList<IPluginSetup> plugins) where T : IPluginSetup, new()
        {
            if (!plugins.Any(p => p is T))
            {
                plugins.Add(new T());
            }
        }
    }
}