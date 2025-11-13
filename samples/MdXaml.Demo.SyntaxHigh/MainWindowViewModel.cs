using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using MdXaml;

namespace MdXaml.Demo.SyntaxHigh
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel()
        {
            Styles = new List<StyleInfo>();

            Styles.Add(new StyleInfo("Plain", null));
            Styles.Add(new StyleInfo("Standard", MarkdownStyle.Standard));
            Styles.Add(new StyleInfo("Compact", MarkdownStyle.Compact));
            Styles.Add(new StyleInfo("GithubLike", MarkdownStyle.GithubLike));
            Styles.Add(new StyleInfo("Sasabune", MarkdownStyle.Sasabune));
            Styles.Add(new StyleInfo("SasabuneStandard", MarkdownStyle.SasabuneStandard));
            Styles.Add(new StyleInfo("SasabuneCompact", MarkdownStyle.SasabuneCompact));

            SelectedStyleInfo = Styles[1];

            var subjectType = typeof(MainWindow);
            var subjectAssembly = GetType().Assembly;
            using (Stream stream = subjectAssembly.GetManifestResourceStream(subjectType.FullName + ".md"))
            {

                if (stream == null)
                {
                    TextView =
                    TextXaml = String.Format("Could not find sample text *{0}*.md", subjectType.FullName);
                }
                else
                {

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        TextView =
                        TextXaml = reader.ReadToEnd();
                    }
                }
                }

            ForegroundRed = 0x00;
            ForegroundGreen = 0x00;
            ForegroundBlue = 0x00;

            BackgroundRed = 0xFF;
            BackgroundGreen = 0xFF;
            BackgroundBlue = 0xFF;
        }


        public StyleInfo _selectedStyleInfo;
        public StyleInfo SelectedStyleInfo
        {
            get { return _selectedStyleInfo; }
            set
            {
                if (_selectedStyleInfo == value) return;
                _selectedStyleInfo = value;
                FirePropertyChanged();
            }
        }

        public List<StyleInfo> _styles;
        public List<StyleInfo> Styles
        {
            get { return _styles; }
            set
            {
                if (_styles == value) return;
                _styles = value;
                FirePropertyChanged();
            }
        }

        public string _textView;
        public string TextView
        {
            get { return _textView; }
            set
            {
                if (_textView == value) return;
                _textView = value;
                FirePropertyChanged();
            }
        }


        private Task TextXamlChangeEvent;
        public string _textXaml;
        public string TextXaml
        {
            get { return _textXaml; }
            set
            {
                if (_textXaml == value) return;
                _textXaml = value;
                if (TextXamlChangeEvent == null || TextXamlChangeEvent.Status >= TaskStatus.RanToCompletion)
                {
                    TextXamlChangeEvent = Task.Run(() =>
                    {
                        Task.Delay(100);
                    retry:
                        var oldVal = _textXaml;

                        Thread.MemoryBarrier();
                        FirePropertyChanged(nameof(TextXaml));

                        Thread.MemoryBarrier();
                        if (oldVal != _textXaml) goto retry;
                    });
                }
            }
        }

        private byte _ForegroundRed;
        public byte ForegroundRed
        {
            get => _ForegroundRed;
            set
            {
                if (_ForegroundRed == value) return;
                _ForegroundRed = value;
                FirePropertyChanged();
            }
        }

        private byte _ForegroundGreen;
        public byte ForegroundGreen
        {
            get => _ForegroundGreen;
            set
            {
                if (_ForegroundGreen == value) return;
                _ForegroundGreen = value;
                FirePropertyChanged();
            }
        }

        private byte _ForegroundBlue;
        public byte ForegroundBlue
        {
            get => _ForegroundBlue;
            set
            {
                if (_ForegroundBlue == value) return;
                _ForegroundBlue = value;
                FirePropertyChanged();
            }
        }

        private byte _BackgroundRed;
        public byte BackgroundRed
        {
            get => _BackgroundRed;
            set
            {
                if (_BackgroundRed == value) return;
                _BackgroundRed = value;
                FirePropertyChanged();
            }
        }

        private byte _BackgroundGreen;
        public byte BackgroundGreen
        {
            get => _BackgroundGreen;
            set
            {
                if (_BackgroundGreen == value) return;
                _BackgroundGreen = value;
                FirePropertyChanged();
            }
        }

        private byte _BackgroundBlue;
        public byte BackgroundBlue
        {
            get => _BackgroundBlue;
            set
            {
                if (_BackgroundBlue == value) return;
                _BackgroundBlue = value;
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
    public class StyleInfo
    {
        public string Name { set; get; }
        public Style Style { set; get; }

        public StyleInfo(string name, Style style)
        {
            Name = name;
            Style = style;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object val)
        {
            if (val is StyleInfo sf)
            {
                return Name == sf.Name;
            }
            else return false;
        }

        public static bool operator ==(StyleInfo left, StyleInfo right)
        {
            if (Object.ReferenceEquals(left, right)) return true;
            if (Object.ReferenceEquals(left, null)) return false;
            return left.Equals(right);
        }

        public static bool operator !=(StyleInfo left, StyleInfo right)
        {
            return !(left == right);
        }
    }
}