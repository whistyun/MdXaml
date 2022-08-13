#if MIG_FREE
namespace Markdown.Xaml
#else
namespace MdXaml
#endif
{
    public interface IPluginSetup
    {
        void Setup(MdXamlPlugins plugins);
    }
}
