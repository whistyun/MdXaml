namespace MdXaml.Html
{
    public class HtmlPluginSetup : IPluginSetup
    {
        private HtmlPlugin _plugin = new HtmlPlugin();

        public void Install(MdXamlPlugins plugins)
        {
            plugins.Block.Add(_plugin);
            plugins.Inline.Add(_plugin);
        }

        public void UnInstall(MdXamlPlugins plugins)
        {
            plugins.Block.Remove(_plugin);
            plugins.Inline.Remove(_plugin);
        }
    }
}
