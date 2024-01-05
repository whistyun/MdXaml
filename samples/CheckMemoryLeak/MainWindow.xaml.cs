using MdXaml.Full;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CheckMemoryLeak
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var vm = (MainWindowViewModel)DataContext;
            foreach (var content in vm.Contents)
            {
                var viewer = new MarkdownScrollViewer();

                if (content.ContentType == ContentType.Resource)
                {
                    viewer.Source = new Uri(content.Text);
                }
                if (content.ContentType == ContentType.Text)
                {
                    viewer.Markdown = content.Text;
                }

                ViewPanel.Children.Add(viewer);

                while (!viewer.IsLoaded)
                    await Task.Delay(1000);

                await Task.Delay(1000);

                ViewPanel.Children.Clear();
            }

            foreach (var i in Enumerable.Range(0, 10)) {
                GC.Collect();
                await Task.Delay(1000);
            }
        }
    }
}