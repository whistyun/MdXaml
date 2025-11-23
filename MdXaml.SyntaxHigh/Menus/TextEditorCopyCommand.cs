using ICSharpCode.AvalonEdit;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace MdXaml.Menus
{
    public class TextEditorCopyCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        private TextEditor _editor;
        private bool _isExecutable;

        public TextEditorCopyCommand(TextEditor editor)
        {
            _editor = editor;
            _editor.ContextMenuOpening += TryUpdateExecutable;
        }

        private void TryUpdateExecutable(object sender, ContextMenuEventArgs e)
        {
            var isExecutable = _editor.SelectionLength != 0;

            if (_isExecutable != isExecutable)
            {
                _isExecutable = isExecutable;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool CanExecute(object parameter)
        {
            return _isExecutable;
        }

        public void Execute(object parameter)
        {
            _editor.Copy();
        }
    }
}
