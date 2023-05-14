

using MdXaml.Plugins;

namespace MdXaml.AnimatedGif
{
    public class AnimatedGifPluginSetup : IPluginSetup
    {
        public void Setup(MdXamlPlugins plugins)
        {
            plugins.ElementLoader.Add(new AnimatedGifLoader());
        }
    }
}
