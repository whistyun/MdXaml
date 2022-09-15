using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace MdXaml.Html.Core.Utils
{
    static class DocUtils
    {
        public static Block CreateCodeBlock(string? lang, string code, ReplaceManager manager)
        {
            var txtEdit = new TextEditor();

            if (!String.IsNullOrEmpty(lang))
            {
                var highlight = HighlightingManager.Instance.GetDefinitionByExtension("." + lang);
                txtEdit.SetCurrentValue(TextEditor.SyntaxHighlightingProperty, highlight);
                txtEdit.Tag = lang;
            }

            txtEdit.Text = code;
            txtEdit.HorizontalAlignment = HorizontalAlignment.Stretch;
            txtEdit.IsReadOnly = true;
            txtEdit.PreviewMouseWheel += (s, e) =>
            {
                if (e.Handled) return;

                e.Handled = true;

                var isShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                if (isShiftDown)
                {
                    // horizontal scroll
                    var offset = txtEdit.HorizontalOffset;
                    offset -= e.Delta;
                    txtEdit.ScrollToHorizontalOffset(offset);
                }
                else
                {
                    // event bubbles
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                    eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                    eventArg.Source = s;

                    var parentObj = ((Control)s).Parent;
                    if (parentObj is UIElement uielm)
                    {
                        uielm.RaiseEvent(eventArg);
                    }
                    else if (parentObj is ContentElement celem)
                    {
                        celem.RaiseEvent(eventArg);
                    }
                }
            };


            var result = new BlockUIContainer(txtEdit);
            result.Tag = manager.GetTag(Tags.TagCodeBlock);

            return result;
        }

        public static void TrimStart(Inline? inline)
        {
            if (inline is null) return;

            if (inline is Span span)
            {
                TrimStart(span.Inlines.FirstOrDefault());
            }
            else if (inline is Run run)
            {
                run.Text = run.Text.TrimStart();
            }
        }

        public static void TrimEnd(Inline? inline)
        {
            if (inline is null) return;

            if (inline is Span span)
            {
                TrimEnd(span.Inlines.LastOrDefault());
            }
            else if (inline is Run run)
            {
                run.Text = run.Text.TrimEnd();
            }
        }
    }
}
