using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Documents;
using System.Windows.Markup;

#if MIG_FREE
namespace Markdown.Xaml
#else
namespace MdXaml
#endif
{
    [ContentProperty(nameof(Setups))]
    public class MdXamlPlugins
    {
        public ObservableCollection<IPluginSetup> Setups { get; }
        public ObservableCollection<IBlockParserPlugin> Block { get; }
        public ObservableCollection<IRunParserPlugin> Inline { get; }

        public MdXamlPlugins()
        {
            Setups = new ObservableCollection<IPluginSetup>();
            Block = new ObservableCollection<IBlockParserPlugin>();
            Inline = new ObservableCollection<IRunParserPlugin>();

            Setups.CollectionChanged += Setups_CollectionChanged;
        }

        private void Setups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!(e.OldItems is null))
                foreach (var removedItem in e.OldItems)
                    ((IPluginSetup)removedItem).UnInstall(this);

            if (!(e.NewItems is null))
                foreach (var addedItem in e.NewItems)
                    ((IPluginSetup)addedItem).Install(this);
        }
    }

    public interface IPluginSetup
    {
        void Install(MdXamlPlugins plugins);
        void UnInstall(MdXamlPlugins plugins);
    }

    public interface IBlockParserPlugin
    {
        IEnumerable<Block> Parse(string text, Func<string, IEnumerable<Block>> defaultHandler);
    }

    public interface IRunParserPlugin
    {
        IEnumerable<Inline> Parse(string text, Func<string, IEnumerable<Inline>> defaultHandler);
    }
}
