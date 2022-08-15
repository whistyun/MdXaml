using MdXaml.Plugins;

namespace MdXaml.Html
{
    public class HtmlPluginSetup : IPluginSetup
    {
        private HtmlBlockParser _block = new HtmlBlockParser();
        private HtmlInlineParser _inline = new HtmlInlineParser();

        public void Setup(MdXamlPlugins plugins)
        {
            plugins.Syntax.EnableNoteBlock = false;
            plugins.TopBlock.Add(_block);
            plugins.Inline.Add(_inline);
        }
    }
}
