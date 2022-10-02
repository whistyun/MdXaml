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
    public class MdXamlPlugins
    {
        public static readonly MdXamlPlugins Default = new();

        public SyntaxManager Syntax { get; }

        public ObservableCollection<IPluginSetup> Setups { get; }
        public ObservableCollection<IBlockParser> TopBlock { get; }
        public ObservableCollection<IBlockParser> Block { get; }
        public ObservableCollection<IInlineParser> Inline { get; }
        public ObservableCollection<IImageLoader> ImageLoader { get; }

        public MdXamlPlugins()
        {
            Syntax = new();

            Setups = new();
            TopBlock = new();
            Block = new();
            Inline = new();
            ImageLoader = new();

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
