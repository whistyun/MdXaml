using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MdXaml.Menus
{
    public static class CommandsForTextEditor
    {

        public static void Setup(TextEditor editor)
        {
            editor.ContextMenuOpening += Editor_ContextMenuOpening;

            var menu = new ContextMenu();
            menu.Items.Add(new MenuItem()
            {
                Name = ApplicationCommands.Copy.Name,
                Header = ApplicationCommands.Copy.Text,
                InputGestureText = ExtractShortcut(ApplicationCommands.Copy.InputGestures),
                Command = new TextEditorCopyCommand(editor)
            });
            menu.Items.Add(new MenuItem()
            {
                Name = ApplicationCommands.SelectAll.Name,
                Header = ApplicationCommands.SelectAll.Text,
                InputGestureText = ExtractShortcut(ApplicationCommands.SelectAll.InputGestures),
                Command = new TextEditorSelectAllCommand(editor)
            });

            editor.ContextMenu = menu;

        }

        private static void Editor_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is TextEditor editor)
            {
                var viewer = WalkParent<FlowDocumentScrollViewer>(editor);

                if (viewer is null)
                    return;

                if (viewer.Selection is not null
                    && !viewer.Selection.IsEmpty)
                {
                    viewer.IsSelectionEnabled = false;
                    viewer.IsSelectionEnabled = true;
                }
            }
        }

        private static T? WalkParent<T>(FrameworkElement start)
            where T : class
        {
            DependencyObject dobj = start.Parent;
            for (; ; )
            {
                if (dobj is T t)
                {
                    return t;
                }
                else if (dobj is FrameworkElement element)
                {
                    dobj = element.Parent;
                }
                else if (dobj is FrameworkContentElement content)
                {
                    dobj = content.Parent;
                }
                else return null;
            }
        }

        private static string? ExtractShortcut(InputGestureCollection? collection)
        {
            if (collection is null)
                return "";

            return collection.OfType<KeyGesture>()
                             .Select(g => g.GetDisplayStringForCulture(CultureInfo.CurrentCulture))
                             .FirstOrDefault()
                   ?? "";
        }
    }
}
