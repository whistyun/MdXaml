using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Documents;
using System.Windows.Markup;

#if MIG_FREE
using Markdown.Xaml.Plugins;
#else
using MdXaml.Plugins;
#endif

#if MIG_FREE
namespace Markdown.Xaml
#else
namespace MdXaml
#endif
{
    [ContentProperty(nameof(Setups))]
    public class MdXamlPlugins
    {
        public static readonly MdXamlPlugins Default = new MdXamlPlugins();

        public SyntaxManager Syntax { get; }

        public ObservableCollection<IPluginSetup> Setups { get; }
        public ObservableCollection<IBlockParser> TopBlock { get; }
        public ObservableCollection<IBlockParser> Block { get; }
        public ObservableCollection<IInlineParser> Inline { get; }

        public MdXamlPlugins()
        {
            Syntax = new SyntaxManager();
            Setups = new ObservableCollection<IPluginSetup>();
            TopBlock = new ObservableCollection<IBlockParser>();
            Block = new ObservableCollection<IBlockParser>();
            Inline = new ObservableCollection<IInlineParser>();

            Setups.CollectionChanged += Setups_CollectionChanged;
        }

        private void Setups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!(e.NewItems is null))
                foreach (var addedItem in e.NewItems)
                    ((IPluginSetup)addedItem).Setup(this);
        }
    }
}
