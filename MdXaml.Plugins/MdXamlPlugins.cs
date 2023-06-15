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

        public event Action Updated;

        public SyntaxManager Syntax { get; }

        public ObservableCollection<IPluginSetup> Setups { get; }
        public ObservableCollection<IBlockParser> TopBlock { get; }
        public ObservableCollection<IBlockParser> Block { get; }
        public ObservableCollection<IInlineParser> Inline { get; }
        public ObservableCollection<IImageLoader> ImageLoader { get; }
        public ObservableCollection<IElementLoader> ElementLoader { get; }
        public ObservableCollection<Definition> Highlights { get; }

        public MdXamlPlugins() : this(new SyntaxManager())
        {
        }

        public MdXamlPlugins(SyntaxManager manager) : this(manager, new(), new(), new(), new(), new(), new(), new())
        {
        }

        private MdXamlPlugins(
            SyntaxManager manager,
            ObservableCollection<IPluginSetup> setups,
            ObservableCollection<IBlockParser> topBlock,
            ObservableCollection<IBlockParser> block,
            ObservableCollection<IInlineParser> inline,
            ObservableCollection<IImageLoader> imageLoader,
            ObservableCollection<IElementLoader> elementLoader,
            ObservableCollection<Definition> highlights)
        {
            Syntax = manager;
            Setups = setups;
            TopBlock = topBlock;
            Block = block;
            Inline = inline;
            ImageLoader = imageLoader;
            ElementLoader = elementLoader;
            Highlights = highlights;

            Setups.CollectionChanged += Setups_CollectionChanged;
            TopBlock.CollectionChanged += (s, e) => NotifyUpdated();
            Block.CollectionChanged += (s, e) => NotifyUpdated();
            Inline.CollectionChanged += (s, e) => NotifyUpdated();
            ImageLoader.CollectionChanged += (s, e) => NotifyUpdated();
            ElementLoader.CollectionChanged += (s, e) => NotifyUpdated();
            Highlights.CollectionChanged += (s, e) => NotifyUpdated();
        }

        private void Setups_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is not null)
                foreach (var addedItem in e.NewItems.Cast<IPluginSetup>())
                    addedItem.Setup(this);

            NotifyUpdated();
        }

        private void NotifyUpdated()
        {
            Updated?.Invoke();
        }

        public MdXamlPlugins Clone()
            => new MdXamlPlugins(
                        Syntax.Clone(),
                        new(Setups),
                        new(TopBlock),
                        new(Block),
                        new(Inline),
                        new(ImageLoader),
                        new(ElementLoader),
                        new(Highlights));

    }
}
