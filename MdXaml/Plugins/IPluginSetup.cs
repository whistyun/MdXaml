namespace MdXaml.Plugins
{
#if MIG_FREE
    internal 
#else
    public
#endif

    interface IPluginSetup
    {
        void Setup(MdXamlPlugins plugins);
    }
}
