using MdXaml.Plugins;
using System;
using System.Collections.Generic;
using System.Text;

namespace MdXaml.Svg
{
    public class SvgPluginSetup : IPluginSetup
    {
        public void Setup(MdXamlPlugins plugins)
        {
            plugins.ImageLoader.Add(new SvgImageLoader());
        }
    }
}
