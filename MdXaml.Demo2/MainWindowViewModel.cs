using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace MdXaml.Demo2
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private Uri _MdSource;
        public Uri MdSource
        {
            get => _MdSource;
            set
            {
                if (_MdSource == value) return;
                _MdSource = value;
                FirePropertyChanged();
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
}
