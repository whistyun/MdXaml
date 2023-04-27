using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MdXaml.Menus
{
    public class TextEditorSelectAllCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        private TextEditor _editor;

        public TextEditorSelectAllCommand(TextEditor editor)
        {
            _editor = editor;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _editor.SelectAll();
        }
    }
}
