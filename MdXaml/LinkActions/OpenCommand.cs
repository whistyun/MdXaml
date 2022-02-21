using System;
using System.Diagnostics;
using System.Windows.Input;

#if MIG_FREE
namespace Markdown.Xaml.LinkActions
#else
namespace MdXaml.LinkActions
#endif
{
    public class OpenCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            var path = parameter.ToString();
            var isAbs = Uri.IsWellFormedUriString(path, UriKind.Absolute);

            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }
}
