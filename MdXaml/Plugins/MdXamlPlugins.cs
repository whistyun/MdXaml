using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Markup;

namespace MdXaml.Plugins
{
    [ContentProperty(nameof(Setups))]
#if MIG_FREE
    internal 
#else
    public
#endif
    class MdXamlPlugins
    {
        public static readonly MdXamlPlugins Default = new();

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

        private void Setups_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is not null)
                foreach (var addedItem in e.NewItems.Cast<IPluginSetup>())
                    addedItem.Setup(this);
        }
    }
}
