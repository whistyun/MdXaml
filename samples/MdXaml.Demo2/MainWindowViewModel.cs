using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace MdXaml.Demo2
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private int ReferenceIdx = 0;

        public ObservableCollection<Uri> Histories { get; } = new ObservableCollection<Uri>();

        public ObservableCollection<MinDoc> Documents { get; }

        private Uri _MdSource;
        public Uri MdSource
        {
            get => _MdSource;
            set
            {
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

            Documents = new ObservableCollection<MinDoc>
            {
                new MinDoc(@"
                    # Title
                    [go to google](https://www.google.com)
                    "),

                new MinDoc(@"
                    # Title
                    [go to yahoo](https://www.yahoo.com/)
                    ")
            };
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

    public class MinDoc
    {
        public string Text { get; set; }

        public MinDoc(string heredoc)
        {
            Text = String.Join("\r\n", Regex.Split(heredoc, "\r?\n").Select(ln => ln.TrimStart()));
        }
    }

    public class Command : ICommand
    {
        private readonly Action Target;

        public event EventHandler CanExecuteChanged;

        public Command(Action a) { Target = a; }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => Target.Invoke();
    }
}
