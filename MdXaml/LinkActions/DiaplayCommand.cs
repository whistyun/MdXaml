using System;
using System.Diagnostics;
using System.Windows.Input;

#if MIG_FREE
using Markdown.Xaml;

namespace Markdown.Xaml.LinkActions
#else
using MdXaml;

namespace MdXaml.LinkActions
#endif
{
    // set `public` access level for #29.
    public class DiaplayCommand : ICommand
    {
        private MarkdownScrollViewer Owner;
        private bool OpenBrowserWithAbsolutePath;

        public DiaplayCommand(MarkdownScrollViewer owner, bool openBrowserWithAbsolutePath)
        {
            Owner = owner;
            OpenBrowserWithAbsolutePath = openBrowserWithAbsolutePath;
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

            var isAbs = Uri.IsWellFormedUriString(path, UriKind.Absolute);

            if (OpenBrowserWithAbsolutePath & isAbs)
            {
                Process.Start(new ProcessStartInfo(path)
                {
                    UseShellExecute = true,
                    Verb = "open"
                });
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
