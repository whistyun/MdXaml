using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using MdXaml.Plugins;
using System.Windows.Media;

using MdXaml.LinkActions;
using MdStyle = MdXaml.MarkdownStyle;
using System.ComponentModel;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace MdXaml
{
    // Markdownを表示するためのControl
    [ContentProperty(nameof(HereMarkdown))]
    public class MarkdownScrollViewer : FlowDocumentScrollViewer, IUriContext
    {
        public static readonly DependencyProperty SourceProperty =
             DependencyProperty.Register(
                 nameof(Source),
                 typeof(Uri),
                 typeof(MarkdownScrollViewer),
                 new PropertyMetadata(null, UpdateSource));

        public static readonly DependencyProperty FragmentProperty =
             DependencyProperty.Register(
                 nameof(Fragment),
                 typeof(string),
                 typeof(MarkdownScrollViewer),
                 new PropertyMetadata(null, UpdateFragment));


        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.Register(
                nameof(Markdown),
                typeof(string),
                typeof(MarkdownScrollViewer),
                new PropertyMetadata("", UpdateMarkdown));


        public static readonly DependencyProperty MarkdownStyleProperty =
            DependencyProperty.Register(
                nameof(MarkdownStyle),
                typeof(Style),
                typeof(MarkdownScrollViewer),
                new PropertyMetadata(MdStyle.Standard, UpdateStyle));

        public static readonly DependencyProperty MarkdownStyleNameProperty =
            DependencyProperty.Register(
                nameof(MarkdownStyleName),
                typeof(string),
                typeof(MarkdownScrollViewer),
                new PropertyMetadata(nameof(MdStyle.Standard), UpdateStyleName));

        public static readonly DependencyProperty AssetPathRootProperty =
            DependencyProperty.Register(
                nameof(AssetPathRoot),
                typeof(string),
                typeof(MarkdownScrollViewer),
                new PropertyMetadata(null, UpdateAssetPathRoot));

        public static readonly DependencyProperty DisabledLazyLoadProperty =
            DependencyProperty.Register(
                nameof(DisabledLazyLoad),
                typeof(bool),
                typeof(MarkdownScrollViewer),
                new PropertyMetadata(false, UpdateDisabledLazyLoad));

        public static readonly DependencyProperty UseSoftlineBreakAsHardlineBreakProperty =
            DependencyProperty.Register(
                nameof(UseSoftlineBreakAsHardlineBreak),
                typeof(bool),
                typeof(MarkdownScrollViewer),
                new PropertyMetadata(false, UpdateUseSoftlineBreakAsHardlineBreak));

        private static void UpdateSource(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownScrollViewer owner && e.NewValue is Uri source)
            {
                if (owner._source != source)
                {
                    owner._source = source;
                    owner.Open(source, false);
                }
                else if (owner.Fragment != source.Fragment)
                {
                    owner.SetCurrentValue(FragmentProperty, source.Fragment);
                }
            }
        }

        private static void UpdateFragment(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownScrollViewer owner && e.NewValue is string fragment)
            {
                if (owner._fragment != fragment)
                {
                    owner._fragment = fragment;
                    owner.ScrollTo(fragment, false);
                }
            }
        }

        private static void UpdateMarkdown(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownScrollViewer owner)
            {
                var plugins = (owner.Plugins ?? MdXamlPlugins.Default).Clone();
                switch (owner.Syntax)
                {
                    case SyntaxVersion.Plain:
                        plugins.Syntax.And(SyntaxManager.Plain);
                        break;

                    case SyntaxVersion.Standard:
                        plugins.Syntax.And(SyntaxManager.Standard);
                        break;

                    case SyntaxVersion.MdXaml:
                        plugins.Syntax.And(SyntaxManager.MdXaml);
                        break;
                }

                owner.Engine.Plugins = plugins;

                var doc = owner.Engine.Transform(owner.Markdown ?? "");
                owner.SetCurrentValue(DocumentProperty, doc);
            }
        }

        private static void UpdateStyle(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownScrollViewer owner)
            {
                owner.Engine.DocumentStyle = (Style)e.NewValue;
            }
        }

        private static void UpdateStyleName(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownScrollViewer owner)
            {
                var newName = (string)e.NewValue;

                if (newName == null) return;

                var prop = typeof(MarkdownStyle).GetProperty(newName);
                if (prop == null) return;

                owner.MarkdownStyle = (Style?)prop.GetValue(null);
            }
        }

        private static void UpdateAssetPathRoot(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownScrollViewer owner)
            {
                var newPath = (string)e.NewValue;
                if (newPath != owner.Engine.AssetPathRoot)
                {
                    owner.Engine.AssetPathRoot = newPath;
                    UpdateMarkdown(d, e);
                }
            }
        }

        private static void UpdateDisabledLazyLoad(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is MarkdownScrollViewer owner)
            {
                var disabledLazyLoad = (bool)e.NewValue;
                if (disabledLazyLoad != owner.Engine.DisabledLazyLoad)
                {
                  owner.Engine.DisabledLazyLoad = (bool)e.NewValue;
                }
            }
        }
        private static void UpdateUseSoftlineBreakAsHardlineBreak(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is MarkdownScrollViewer owner)
            {
                var useSoftlineBreakAsHardlineBreak = (bool)e.NewValue;
                if (useSoftlineBreakAsHardlineBreak != owner.Engine.UseSoftlineBreakAsHardlineBreak)
                {
                  owner.Engine.UseSoftlineBreakAsHardlineBreak = (bool)e.NewValue;
                }
            }
        }

        private string _fragment;
        private Uri _source;

        private Markdown _engine;
        public Markdown Engine
        {
            set
            {
                _engine = value;

                if (BaseUri != null)
                    _engine.BaseUri = BaseUri;

                if (AssetPathRoot != null)
                    _engine.AssetPathRoot = AssetPathRoot;

                if (MarkdownStyle != null)
                    _engine.DocumentStyle = MarkdownStyle;

                UpdateClickAction();
                UpdateMarkdown(this, default);
            }
            get => _engine;
        }

        private Uri? _baseUri;
        public Uri? BaseUri
        {
            set
            {
                _baseUri = value;
                Engine.BaseUri = value;
            }
            get => _baseUri ?? Engine.BaseUri;
        }

        public string AssetPathRoot
        {
            set => SetValue(AssetPathRootProperty, value);
            get => (string)GetValue(AssetPathRootProperty);
        }

        private MdXamlPlugins? _plugins;
        public virtual MdXamlPlugins? Plugins
        {
            set
            {
                _plugins = value;
                UpdateMarkdown(this, default);
            }
            get
            {
                if (_plugins is not null)
                    return _plugins;

                // load from application.resource
                var values = Application.Current?.Resources?.Values;
                if (values is null) return null;

                return _plugins = values.OfType<MdXamlPlugins>().FirstOrDefault();
            }
        }


        public string HereMarkdown
        {
            get { return Markdown; }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    Markdown = value;
                }
                else
                {
                    // like PHP's flexible_heredoc_nowdoc_syntaxes,
                    // The indentation of the closing tag dictates 
                    // the amount of whitespace to strip from each line 
                    var lines = Regex.Split(value, "\r\n|\r|\n", RegexOptions.Multiline);

                    // count last line indent
                    int lastIdtCnt = TextUtil.CountIndent(lines.Last());
                    // count full indent
                    int someIdtCnt = lines
                        .Where(line => !String.IsNullOrWhiteSpace(line))
                        .Select(line => TextUtil.CountIndent(line))
                        .Min();

                    var indentCount = Math.Max(lastIdtCnt, someIdtCnt);

                    Markdown = String.Join(
                        "\n",
                        lines
                            // skip first blank line
                            .Skip(String.IsNullOrWhiteSpace(lines[0]) ? 1 : 0)
                            // strip indent
                            .Select(line =>
                            {
                                var realIdx = 0;
                                var viewIdx = 0;

                                while (viewIdx < indentCount && realIdx < line.Length)
                                {
                                    var c = line[realIdx];
                                    if (c == ' ')
                                    {
                                        realIdx += 1;
                                        viewIdx += 1;
                                    }
                                    else if (c == '\t')
                                    {
                                        realIdx += 1;
                                        viewIdx = ((viewIdx >> 2) + 1) << 2;
                                    }
                                    else break;
                                }

                                return line.Substring(realIdx);
                            })
                        );
                }
            }
        }

        public string Markdown
        {
            get { return (string)GetValue(MarkdownProperty); }
            set { SetValue(MarkdownProperty, value); }
        }

        public Style? MarkdownStyle
        {
            get { return (Style)GetValue(MarkdownStyleProperty); }
            set { SetValue(MarkdownStyleProperty, value); }
        }

        public string MarkdownStyleName
        {
            get { return (string)GetValue(MarkdownStyleNameProperty); }
            set { SetValue(MarkdownStyleNameProperty, value); }
        }

        public bool DisabledLazyLoad
        {
          get { return (bool)GetValue(DisabledLazyLoadProperty); }
          set { SetValue(DisabledLazyLoadProperty, value); }
        }

        public Uri? Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public string Fragment
        {
            get { return (string)GetValue(FragmentProperty); }
            set { SetValue(FragmentProperty, value); }
        }
        public bool UseSoftlineBreakAsHardlineBreak
        {
          get { return (bool)GetValue(UseSoftlineBreakAsHardlineBreakProperty); }
          set { SetValue(UseSoftlineBreakAsHardlineBreakProperty, value); }
        }

        private ClickAction _clickAction;
        public ClickAction ClickAction
        {
            get { return _clickAction; }
            set
            {
                _clickAction = value;
                UpdateClickAction();
                UpdateMarkdown(this, default);
            }
        }

        private SyntaxVersion _syntax;
        public SyntaxVersion Syntax
        {
            get => _syntax;
            set
            {
                _syntax = value;
                UpdateMarkdown(this, default);
            }
        }

        private HyperLinkClickCallback _onHyperLinkClicked;
        public HyperLinkClickCallback OnHyperLinkClicked
        {
            get => _onHyperLinkClicked;
            set
            {
                _onHyperLinkClicked = value;
                Engine.OnHyperLinkClicked = value;
            }
        }

        public new FlowDocument? Document
        {
            get
            {
                var bs = (FlowDocumentScrollViewer)this;
                return bs.Document ??= new FlowDocument();
            }
            set => ((FlowDocumentScrollViewer)this).Document = value;
        }


        public MarkdownScrollViewer()
        {
            _engine = new Markdown();
            _syntax = SyntaxVersion.MdXaml;

            if (BaseUri != null)
                _engine.BaseUri = BaseUri;

            if (AssetPathRoot != null)
                _engine.AssetPathRoot = AssetPathRoot;

            if (MarkdownStyle != null)
                _engine.DocumentStyle = MarkdownStyle;

            UpdateClickAction();


            var menu = new ContextMenu();
            menu.Items.Add(ApplicationCommands.Copy);
            menu.Items.Add(ApplicationCommands.SelectAll);

            ContextMenu = menu;


            // Do not use DependencyPropertyDescriptor. This may cause memory leaks if used with low understanding.
            // 
            // DependencyPropertyDescriptor
            //     .FromProperty(FlowDocumentScrollViewer.DocumentProperty, typeof(FlowDocumentScrollViewer))
            //     .AddValueChanged(this, OnDocumentChanged);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == FlowDocumentScrollViewer.DocumentProperty) 
                OnDocumentChanged();
        }


        private void OnDocumentChanged()
        {
            if (Document is not null)
                ScrollTo(Fragment, false);
        }

        private void UpdateClickAction()
        {
            ICommand command;
            switch (_clickAction)
            {
                case ClickAction.OpenBrowser:
                    command = new OpenCommand();
                    break;

                case ClickAction.DisplayWithRelativePath:
                    command = new DisplayCommand(this, true, false);
                    break;

                case ClickAction.DisplayAll:
                    command = new DisplayCommand(this, false, false);
                    break;

                case ClickAction.SafetyOpenBrowser:
                    command = new SafetyOpenCommand();
                    break;

                case ClickAction.SafetyDisplayWithRelativePath:
                    command = new DisplayCommand(this, true, true);
                    break;

                case ClickAction.HighlightOnly:
                    Engine.HyperlinkCommand = new HighlightOnlyCommand();
                    Engine.OnHyperLinkClicked = _onHyperLinkClicked;
                    return;

                default:
                    return;
            }

            Engine.HyperlinkCommand = new FlowDocumentJumpAnchorIfNecessary(this, command);
            Engine.OnHyperLinkClicked = _onHyperLinkClicked;
        }

        internal void ScrollTo(string fragment, bool updateSourceProperty)
        {
            if (updateSourceProperty)
            {
                _fragment = fragment;
                SetCurrentValue(FragmentProperty, fragment);
            }


            if (String.IsNullOrEmpty(fragment))
                return;

            if (Document is null)
            {
                Debug.Print($"MarkdownScrollViewer is uninitialized.");
                return;
            }

            var identifier = fragment.StartsWith("#") ?
                                    fragment.Substring(1) :
                                    fragment;

            var anchor = DocumentAnchor.FindAnchor(Document, identifier);
            if (anchor is null)
            {
                Debug.Print($"Not found linkanchor: {identifier}");
                return;
            }

            if (anchor.IsLoaded)
            {
                /*
                 * dirty hack
                 * 
                 * I have no idea to detect a text element position in ScrollViewer.
                 * BringIntoView has no effect if text element is placed in Viewport,
                 * so scroll to the top once.
                 */
                var scroll = GetScrollViewer();
                scroll?.ScrollToTop();
                Dispatcher.Invoke(() => anchor.BringIntoView(), DispatcherPriority.Render);
            }
            else
                anchor.Loaded += (s, e) =>
                {
                    /*
                     * dirty hack
                     * 
                     * BringIntoView fails to scroll at correct position when Loaded only.
                     */
                    Dispatcher.Invoke(async () =>
                    {
                        await Task.Delay(100);
                        Dispatcher.Invoke(anchor.BringIntoView, DispatcherPriority.Background);
                    }, DispatcherPriority.Background);
                };

        }

        internal void Open(Uri source, bool updateSourceProperty)
        {
            bool TryOpen(Uri path)
            {
                try
                {
                    string newMdTxt;

                    switch (path.Scheme)
                    {
                        case "http":
                        case "https":
                            using (var wc = new WebClient())
                            using (var strm = new MemoryStream(wc.DownloadData(path)))
                            using (var reader = new StreamReader(strm, true))
                                newMdTxt = reader.ReadToEnd();
                            break;

                        case "file":
                            using (var strm = File.OpenRead(path.LocalPath))
                            using (var reader = new StreamReader(strm, true))
                                newMdTxt = reader.ReadToEnd();
                            break;

                        case "pack":
                            using (var strm = Application.GetResourceStream(path).Stream)
                            using (var reader = new StreamReader(strm, true))
                                newMdTxt = reader.ReadToEnd();
                            break;

                        default:
                            throw new ArgumentException($"unsupport schema {path.Scheme}");
                    }

                    var assetPathRoot = path.Scheme == "file" ? Path.GetDirectoryName(path.LocalPath) : path.AbsoluteUri;

                    // suppress property changed
                    Engine.AssetPathRoot = assetPathRoot;
                    SetCurrentValue(AssetPathRootProperty, assetPathRoot);

                    _fragment = path.Fragment;
                    SetCurrentValue(FragmentProperty, path.Fragment);

                    if (updateSourceProperty)
                    {
                        _source = path;
                        SetCurrentValue(SourceProperty, path);
                    }

                    SetCurrentValue(MarkdownProperty, newMdTxt);

                    return true;
                }
                catch
                {
                    return false;
                }
            }

            if (source.IsAbsoluteUri)
            {
                TryOpen(source);
            }
            else if (BaseUri is null)
            {
                Debug.WriteLine($"Failed to open markdown from relative path '{source}': BaseUri is null");
            }
            else if (!TryOpen(new Uri(BaseUri, source)))
            {
                if (Uri.IsWellFormedUriString(AssetPathRoot, UriKind.Absolute))
                {
                    var assetUri = new Uri(new Uri(AssetPathRoot), source);
                    TryOpen(assetUri);
                }
                else
                {
                    var assetPath = Path.Combine(AssetPathRoot, source.ToString());
                    TryOpen(new Uri(assetPath));
                }
            }
            else
            {
                Debug.WriteLine($"Failed to open markdown from relative path '{source}': not found");
            }
        }

        private ScrollViewer GetScrollViewer()
        {
            ScrollViewer Find(Visual visual)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
                {
                    var child = VisualTreeHelper.GetChild(visual, i);

                    if (child is ScrollViewer scroll)
                        return scroll;

                    if (child is Visual vis)
                        return Find(vis);
                }

                return null;
            }

            return Find(this);
        }
    }

    public enum ClickAction
    {
        None,
        OpenBrowser,
        DisplayWithRelativePath,
        DisplayAll,
        SafetyOpenBrowser,
        SafetyDisplayWithRelativePath,
        HighlightOnly,
    }

    public enum SyntaxVersion
    {
        Plain,
        Standard,
        MdXaml
    }
}
