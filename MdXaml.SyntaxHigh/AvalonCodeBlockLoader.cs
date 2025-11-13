using ICSharpCode.AvalonEdit;
using MdXaml.Highlighting;
using MdXaml.Menus;
using MdXaml.Plugins;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MdXaml.SyntaxHigh
{
    public class AvalonCodeBlockLoader : ICodeBlockLoader
    {

        private InternalHighlightManager HighlightManager { get; } = new();

        public void Register(Definition definition)
        {
            this.HighlightManager.Register(definition);
        }

        public FrameworkElement CodeBlocksEvaluator(string? lang, string code, bool disabledContextMenu)
        {
            var txtEdit = new TextEditor();

            if (!String.IsNullOrEmpty(lang))
            {
                var highlight = HighlightManager.Get(lang!);
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
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                    {
                        RoutedEvent = UIElement.MouseWheelEvent,
                        Source = s,
                    };

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

            if (!disabledContextMenu)
            {
                CommandsForTextEditor.Setup(txtEdit);
            }

            return txtEdit;
        }
    }
}
