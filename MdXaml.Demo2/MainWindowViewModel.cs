using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace MdXaml.Demo2
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private int ReferenceIdx = 0;

        public ObservableCollection<Uri> Histories { get; } = new ObservableCollection<Uri>();

        private Uri _MdSource;
        public Uri MdSource
        {
            get => _MdSource;
            set
            {
                if (_MdSource == value) return;
                _MdSource = value;

                Histories.Add(value);
                ReferenceIdx = Histories.Count - 1;

                FirePropertyChanged();
            }
        }

        public ICommand Prev { get; }
        public ICommand Next { get; }

        public MainWindowViewModel()
        {
            Prev = new Command(PrevPage);
            Next = new Command(NextPage);

            MdSource = new Uri("Assets/Main.md", UriKind.Relative);

        }

        public void NextPage()
        {
            if (ReferenceIdx < Histories.Count - 1)
            {
                _MdSource = Histories[++ReferenceIdx];
                FirePropertyChanged(nameof(MdSource));
            }
        }

        public void PrevPage()
        {
            if (ReferenceIdx > 0)
            {
                _MdSource = Histories[--ReferenceIdx];
                FirePropertyChanged(nameof(MdSource));
            }
        }


        /// <summary> <see cref="INotifyPropertyChanged"/> </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// <see cref="INotifyPropertyChanged"/>のイベント発火用
        /// </summary>
        /// <param name="propertyName"></param>
        protected void FirePropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null && propertyName != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, e);
            }
        }
    }

    public class Command : ICommand
    {
        private Action Target;

        public event EventHandler CanExecuteChanged;

        public Command(Action a) { Target = a; }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => Target.Invoke();
    }
}
