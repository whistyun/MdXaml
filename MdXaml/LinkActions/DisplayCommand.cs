using System;
using System.Diagnostics;
using System.Windows.Input;

using MdXaml;

namespace MdXaml.LinkActions
{
    // set `public` access level for #29.
    public class DisplayCommand : ICommand
    {
        private MarkdownScrollViewer Owner;
        private bool OpenBrowserWithAbsolutePath;
        private ICommand OpenCommand;

        public DisplayCommand(MarkdownScrollViewer owner, bool openBrowserWithAbsolutePath, bool safety)
        {
            Owner = owner;
            OpenBrowserWithAbsolutePath = openBrowserWithAbsolutePath;
            OpenCommand = safety ? new SafetyOpenCommand() : new OpenCommand();
        }

        public event EventHandler? CanExecuteChanged;

        private bool _isExecutable = true;
        public bool IsExecutable
        {
            get => _isExecutable;
            set
            {
                if (_isExecutable != value)
                {
                    _isExecutable = value;
                    CanExecuteChanged?.Invoke(this, new EventArgs());
                }
            }
        }

        public bool CanExecute(object? parameter) => _isExecutable;

        public void Execute(object? parameter)
        {
            var path = parameter?.ToString();
            if (path is null) throw new ArgumentNullException(nameof(parameter));

            if (path.StartsWith("file:///"))
            {
                path = path.Replace('\\', '/');
            }

            var isAbs = Uri.IsWellFormedUriString(path, UriKind.Absolute);

            if (OpenBrowserWithAbsolutePath & isAbs)
            {
                OpenCommand.Execute(path);
            }
            else if (isAbs)
            {
                Owner.Open(new Uri(path), true);
            }
            else
            {
                Owner.Open(new Uri(path, UriKind.Relative), true);
            }
        }
    }
}
