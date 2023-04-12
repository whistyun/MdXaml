using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MdXaml
{
    public class DocumentAnchor
    {
        public static readonly DependencyProperty HyperlinkAnchorProperty =
            DependencyProperty.RegisterAttached(
                "HyperlinkAnchor",
                typeof(string),
                typeof(DocumentAnchor),
                new PropertyMetadata(null));

        public static void SetHyperlinkAnchor(DependencyObject dobj, string text)
            => dobj.SetValue(HyperlinkAnchorProperty, text);

        public static string GetHyperlinkAnchor(DependencyObject dobj)
            => (string)dobj.GetValue(HyperlinkAnchorProperty);

        public static TextElement? FindAnchor(FlowDocument doc, string identifier)
        {
            var generatedId = new HashSet<string>();

            foreach (var element in WalkElement(doc))
            {
                if (GetHyperlinkAnchor(element) is string anchorText)
                {
                    if (identifier == anchorText)
                        return element;
                }

                if (element is Paragraph paragraph
                 && paragraph.Tag is string tagText
                 && tagText.StartsWith("Heading"))
                {
                    var paragraphText = GetTextFrom(paragraph);
                    paragraphText = GenerateId(paragraphText, generatedId);

                    if (identifier == paragraphText)
                        return element;
                }
            }

            return null;
        }

        private static IEnumerable<TextElement> WalkElement(FlowDocument doc)
        {
            TextPointer element = doc.ContentStart;
            while (element is not null)
            {
                if (element.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart)
                    if (element.Parent is TextElement textElement)
                        yield return textElement;

                element = element.GetNextContextPosition(LogicalDirection.Forward);
            }
        }


        private static string GenerateId(string paragraphText, HashSet<string> generated)
        {
            // https://github.com/xoofx/markdig/blob/master/src/Markdig.Tests/Specs/AutoIdentifierSpecs.md

            var buff = new StringBuilder();

            foreach (var c in paragraphText)
            {
                if (char.IsLetter(c))
                {
                    buff.Append(Char.ToLower(c));
                }
                else if (buff.Length > 0)
                {
                    if (char.IsDigit(c))
                    {
                        buff.Append(Char.ToLower(c));
                    }
                    else if (c == '.' || c == '-' || c == '_')
                    {
                        if (buff[buff.Length - 1] == c)
                            continue;

                        buff.Append(c);
                    }
                    else if (c == ' ')
                    {
                        if (buff[buff.Length - 1] == '-')
                            continue;

                        buff.Append('-');
                    }
                }
            }

            while (buff.Length > 0 && !char.IsLetterOrDigit(buff[buff.Length - 1]))
                buff.Length--;

            if (buff.Length == 0)
                buff.Append("section");


            if (generated.Add(buff.ToString()))
            {
                return buff.ToString();
            }
            else
            {
                buff.Append('-');

                var lenBack = buff.Length;

                foreach (var num in Enumerable.Range(1, 100000))
                {
                    buff.Append(num);

                    if (generated.Add(buff.ToString()))
                        return buff.ToString();

                    buff.Length = lenBack;
                }

                return buff.ToString();
            }
        }


        private static string GetTextFrom(Paragraph paragraph)
        {
            var builder = new StringBuilder();

            foreach (var child in paragraph.Inlines)
                GetTextFrom(child, builder);

            return builder.ToString();

            static void GetTextFrom(Inline inline, StringBuilder outto)
            {
                if (inline is Run run)
                {
                    outto.Append(run.Text);
                }
                else if (inline is LineBreak)
                {
                    outto.AppendLine();
                }
                else if (inline is Span span)
                {
                    foreach (var descendant in span.Inlines)
                        GetTextFrom(descendant, outto);
                }
            }
        }
    }
}
