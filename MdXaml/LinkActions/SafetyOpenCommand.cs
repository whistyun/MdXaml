using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace MdXaml.LinkActions
{
    internal class SafetyOpenCommand : ICommand
    {
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

        public bool CanExecute(object? parameter)
            => _isExecutable && !string.IsNullOrWhiteSpace(parameter?.ToString());

        public void Execute(object? parameter)
        {
            var path = parameter?.ToString();
            if (path is null) throw new ArgumentNullException(nameof(parameter));


            if (!path.StartsWith("http://") && !path.StartsWith("https://"))
            {
                var result = MessageBox.Show($"Execute?\r\n'{path}'", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                    return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(path)
                {
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception e)
            {
                MessageBox.Show($"{e.Message}\r\n'{path}'", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
