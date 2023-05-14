using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace MdXaml.Plugins
{
    public interface IMarkdown
    {
        public ICommand? HyperlinkCommand { get; }

        public Uri? BaseUri { get; }

        public string? AssetPathRoot { get; }

        public InlineUIContainer LoadImage(
            string? tag, string urlTxt, string? tooltipTxt,
            Action<InlineUIContainer, Image?, ImageSource?>? onSuccess = null);

        FlowDocument Transform(string text);

        IEnumerable<Block> RunBlockGamut(string text, bool supportTextAlignment);

        IEnumerable<Inline> RunSpanGamut(string text);
    }
}
