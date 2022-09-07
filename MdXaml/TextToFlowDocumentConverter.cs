using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

#if MIG_FREE
namespace Markdown.Xaml
#else
namespace MdXaml
#endif
{
    public class TextToFlowDocumentConverter : DependencyObject, IValueConverter
    {
        // Using a DependencyProperty as the backing store for Markdown.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.Register("Markdown", typeof(Markdown), typeof(TextToFlowDocumentConverter), new PropertyMetadata(null, MarkdownUpdate));

        private static void MarkdownUpdate(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not null)
            {
                var owner = (TextToFlowDocumentConverter)d;
                if (owner.MarkdownStyle != null)
                {
                    ((Markdown)e.NewValue).DocumentStyle = owner.MarkdownStyle;
                }
            }
        }

        private Lazy<Markdown> _markdown;
        private Style? _markdownStyle;

        public TextToFlowDocumentConverter()
        {
            _markdown = new Lazy<Markdown>(MakeMarkdown);
        }

        public Markdown Markdown
        {
            get { return (Markdown)GetValue(MarkdownProperty); }
            set { SetValue(MarkdownProperty, value); }
        }
        public Style? MarkdownStyle
        {
            get { return _markdownStyle; }
            set
            {
                _markdownStyle = value;
                if (value is not null && Markdown is not null)
                {
                    Markdown.DocumentStyle = value;
                }
            }
        }

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return null;
            }

            var text = (string)value;

            var engine = Markdown ?? _markdown.Value;

            return engine.Transform(text);
        }

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private Markdown MakeMarkdown()
        {
            var markdown = new Markdown();
            if (MarkdownStyle is not null)
            {
                markdown.DocumentStyle = MarkdownStyle;
            }
            return markdown;
        }
    }
}
