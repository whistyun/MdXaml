using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Net.Mime;
using System.Text;
using System.Windows.Input;

namespace CheckMemoryLeak
{
    internal class MainWindowViewModel
    {
        public ICommand AddTextCommand { get; }
        public ICommand AddResourceCommand { get; }

        public ObservableCollection<Content> Contents { get; }
        public MainWindowViewModel()
        {
            Contents = new();
            AddTextCommand = new DelegateCommand(AddText);
            AddResourceCommand = new DelegateCommand(AddResource);

            Contents.Add(new Content(ContentType.Resource) { Text = "https://raw.githubusercontent.com/whistyun/MdXaml/master/samples/MdXaml.Demo/MainWindow.md#note" });
            Contents.Add(new Content(ContentType.Resource) { Text = @"file:///D:\MdXaml\samples\MdXaml.Demo\MainWindow.md#note" });
            Contents.Add(new Content(ContentType.Text) { Text = @"[text](file:///D:\MdXaml\samples\MdXaml.Demo\MainWindow.md#note)" });
        }

        public void AddText()
        {
            Contents.Add(new Content(ContentType.Text));
        }
        public void AddResource()
        {
            Contents.Add(new Content(ContentType.Resource));
        }
    }

    public class DelegateCommand : ICommand
    {
        private Action _action;
        public event EventHandler? CanExecuteChanged;

        public DelegateCommand(Action act)
        {
            _action = act;
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            _action();
        }
    }

    public class Content
    {
        public ContentType ContentType { get; }
        public string Text { get; set; }

        public Content(ContentType type)
        {
            ContentType = type;
        }
    }

    public enum ContentType
    {
        Resource,
        Text
    }
}
