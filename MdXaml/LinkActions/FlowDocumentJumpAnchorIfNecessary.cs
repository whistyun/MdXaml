using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;

namespace MdXaml.LinkActions
{
    // set `public` access level for #29.
    public class FlowDocumentJumpAnchorIfNecessary : ICommand
    {
        private MarkdownScrollViewer _viewer;
        private ICommand _command;

        public event EventHandler? CanExecuteChanged
        {
            add => _command.CanExecuteChanged += value;
            remove => _command.CanExecuteChanged -= value;
        }

        public FlowDocumentJumpAnchorIfNecessary(MarkdownScrollViewer viewer, ICommand defaultAct)
        {
            _viewer = viewer;
            _command = defaultAct;
        }

        public bool CanExecute(object? parameter) => _command.CanExecute(parameter);

        public void Execute(object? parameter)
        {
            if (parameter is string linkText && linkText.StartsWith("#"))
            {
                var identifier = linkText.Substring(1);
                if (_viewer.Document is null)
                {
                    Debug.Print($"MarkdownScrollViewer is uninitialized.");
                    return;
                }

                var anchor = DocumentAnchor.FindAnchor(_viewer.Document, identifier);

                if (anchor is null)
                {
                    Debug.Print($"Not found linkanchor: {identifier}");
                    return;
                }

                anchor.BringIntoView();
            }
            else _command.Execute(parameter);
        }
    }
}
