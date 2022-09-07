using MdXaml.Plugins;

namespace MdXaml.Html
{
    public class HtmlPluginSetup : IPluginSetup
    {
        private readonly HtmlBlockParser _block = new();
        private readonly HtmlInlineParser _inline = new();

        public void Setup(MdXamlPlugins plugins)
        {
            plugins.Syntax.EnableNoteBlock = false;
            plugins.TopBlock.Add(_block);
            plugins.Inline.Add(_inline);
        }
    }
}
