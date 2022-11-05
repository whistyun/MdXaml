using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace VisualTestApp
{
    internal class MainWIndowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _assetPathRoot;
        public string AssetPathRoot
        {
            get => _assetPathRoot;
            set
            {
                _assetPathRoot = value;
                var e = new PropertyChangedEventArgs(nameof(AssetPathRoot));
                PropertyChanged?.Invoke(this, e);
            }
        }

        private string _markdownPath;
        public string MarkdownPath {
            get => _markdownPath;
            set
            {
                _markdownPath = value;
                var e = new PropertyChangedEventArgs(nameof(MarkdownPath));
                PropertyChanged?.Invoke(this, e);
            }
        }
    }
}
