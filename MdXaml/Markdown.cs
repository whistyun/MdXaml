using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MdXaml.Plugins;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;
using MdXaml.Menus;
using System.Globalization;

// I will not add System.Index and System.Range. There is not exist with net45.
#pragma warning disable IDE0056
#pragma warning disable IDE0057

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using MdXaml.Highlighting;

namespace MdXaml
{
    public class Markdown : DependencyObject, IMarkdown, IUriContext
    {
        #region const
        /// <summary>
        /// maximum nested depth of [] and () supported by the transform; implementation detail
        /// </summary>
        private const int _nestDepth = 6;

        private const string TagHeading1 = "Heading1";
        private const string TagHeading2 = "Heading2";
        private const string TagHeading3 = "Heading3";
        private const string TagHeading4 = "Heading4";
        private const string TagHeading5 = "Heading5";
        private const string TagHeading6 = "Heading6";
        private const string TagCode = "CodeSpan";
        private const string TagCodeBlock = "CodeBlock";
        private const string TagBlockquote = "Blockquote";
        private const string TagNote = "Note";
        private const string TagTableHeader = "TableHeader";
        private const string TagTableBody = "TableBody";
        private const string TagOddTableRow = "OddTableRow";
        private const string TagEvenTableRow = "EvenTableRow";

        private const string TagBoldSpan = "Bold";
        private const string TagItalicSpan = "Italic";
        private const string TagStrikethroughSpan = "Strikethrough";
        private const string TagUnderlineSpan = "Underline";

        private const string TagRuleSingle = "RuleSingle";
        private const string TagRuleDouble = "RuleDouble";
        private const string TagRuleBold = "RuleBold";
        private const string TagRuleBoldWithSingle = "RuleBoldWithSingle";

        #endregion

        /// <summary>
        /// when true, bold and italic require non-word characters on either side  
        /// WARNING: this is a significant deviation from the markdown spec
        /// </summary>
        public bool StrictBoldItalic { get; set; }

        public bool DisabledTag { get; set; }

        public bool DisabledTootip { get; set; }

        public bool DisabledLazyLoad { get; set; }

        public bool DisabledContextMenu { get; set; }

        public bool UseSoftlineBreakAsHardlineBreak { get; set; }

        public string? AssetPathRoot { get; set; }

        public ICommand? HyperlinkCommand { get; set; }

        public HyperLinkClickCallback? OnHyperLinkClicked { get; set; }

        public Uri? BaseUri { get; set; }

        private MdXamlPlugins? _plugins;
        public virtual MdXamlPlugins? Plugins
        {
            get => _plugins;
            set
            {
                if (_plugins == value)
                    return;

                if (_plugins is not null)
                    _plugins.Updated -= PluginUpdated;

                _plugins = value ?? MdXamlPlugins.Default;
                _plugins.Updated += PluginUpdated;
                PluginUpdated();
            }
        }

        private ParseParam ParseParam { get; set; }

        private ImageLoaderManager LoaderManager { get; }

        #region dependencyobject property

        // Using a DependencyProperty as the backing store for DocumentStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DocumentStyleProperty =
            DependencyProperty.Register(nameof(DocumentStyle), typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        /// <summary>
        /// top-level flow document style
        /// </summary>
        public Style? DocumentStyle
        {
            get { return (Style)GetValue(DocumentStyleProperty); }
            set { SetValue(DocumentStyleProperty, value); }
        }

        #endregion

        #region legacy property

        public Style? Heading1Style { get; set; }
        public Style? Heading2Style { get; set; }
        public Style? Heading3Style { get; set; }
        public Style? Heading4Style { get; set; }
        public Style? Heading5Style { get; set; }
        public Style? Heading6Style { get; set; }
        public Style? NormalParagraphStyle { get; set; }
        public Style? CodeStyle { get; set; }
        public Style? CodeBlockStyle { get; set; }
        public Style? BlockquoteStyle { get; set; }
        public Style? LinkStyle { get; set; }
        public Style? ImageStyle { get; set; }
        public Style? SeparatorStyle { get; set; }
        public Style? TableStyle { get; set; }
        public Style? TableHeaderStyle { get; set; }
        public Style? TableBodyStyle { get; set; }
        public Style? NoteStyle { get; set; }

        #endregion

        public Markdown()
        {
            HyperlinkCommand = NavigationCommands.GoToPage;
            AssetPathRoot = Environment.CurrentDirectory;
            LoaderManager = new();
            Plugins = MdXamlPlugins.Default;
        }

        public FlowDocument Transform(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            text = TextUtil.Normalize(text);
            var document = Create<FlowDocument, Block>(PrivateRunBlockGamut(text, ParseParam.SupportTextAlignment));

            document.SetBinding(FlowDocument.StyleProperty, new Binding(DocumentStyleProperty.Name) { Source = this });

            return document;
        }

        private void PluginUpdated()
        {
            var topBlocks = new List<IBlockParser>();
            var subBlocks = new List<IBlockParser>();
            var inlines = new List<IInlineParser>();

            // top-level block parser

            if (_plugins.Syntax.EnableListMarkerExt)
            {
                topBlocks.Add(SimpleBlockParser.New(_extListNested, ExtListEvaluator));
            }
            else
            {
                topBlocks.Add(SimpleBlockParser.New(_commonListNested, CommonListEvaluator));
            }

            topBlocks.Add(SimpleBlockParser.New(_codeBlockFirst, CodeBlocksWithLangEvaluator));

            // sub-level block parser

            subBlocks.Add(SimpleBlockParser.New(_blockquoteFirst, BlockquotesEvaluator));
            subBlocks.Add(SimpleBlockParser.New(_headerSetext, SetextHeaderEvaluator));
            subBlocks.Add(SimpleBlockParser.New(_headerAtx, AtxHeaderEvaluator));


            if (_plugins.Syntax.EnableRuleExt)
            {
                subBlocks.Add(SimpleBlockParser.New(_horizontalRules, RuleEvaluator));
            }
            else
            {
                subBlocks.Add(SimpleBlockParser.New(_horizontalCommonRules, RuleCommonEvaluator));
            }

            if (_plugins.Syntax.EnableTableBlock)
            {
                subBlocks.Add(SimpleBlockParser.New(_table, TableEvalutor));
            }
            if (_plugins.Syntax.EnableNoteBlock)
            {
                subBlocks.Add(SimpleBlockParser.New(_note, NoteEvaluator));
            }
            subBlocks.Add(SimpleBlockParser.New(_indentCodeBlock, CodeBlocksWithoutLangEvaluator));

            // inline parser

            inlines.Add(SimpleInlineParser.New(_codeSpan, CodeSpanEvaluator));

            if (_plugins.Syntax.EnableImageResizeExt)
            {
                inlines.Add(SimpleInlineParser.New(_resizeImage, ImageWithSizeEvaluator));
            }
            inlines.Add(SimpleInlineParser.New(_imageOrHrefInline, ImageOrHrefInlineEvaluator));

            if (StrictBoldItalic)
            {
                inlines.Add(SimpleInlineParser.New(_strictBold, BoldEvaluator));
                inlines.Add(SimpleInlineParser.New(_strictItalic, ItalicEvaluator));

                if (_plugins.Syntax.EnableStrikethrough)
                    inlines.Add(SimpleInlineParser.New(_strikethrough, StrikethroughEvaluator));
            }

            topBlocks.AddRange(_plugins.TopBlock);
            subBlocks.AddRange(_plugins.Block);
            inlines.AddRange(_plugins.Inline);

            var manager = new InternalHighlightManager();
            foreach (var def in _plugins.Highlights)
                manager.Register(def);

            ParseParam = new ParseParam(topBlocks, subBlocks, inlines, _plugins.Syntax, manager);
            LoaderManager.Restructure(_plugins);
        }

        /// <summary>
        /// Perform transformations that form block-level tags like paragraphs, headers, and list items.
        /// </summary>
        public IEnumerable<Block> RunBlockGamut(string text, bool supportTextAlignment)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return PrivateRunBlockGamut(text, supportTextAlignment);
        }
        /// <summary>
        /// Perform transformations that occur *within* block-level tags like paragraphs, headers, and list items.
        /// </summary>
        public IEnumerable<Inline> RunSpanGamut(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return PrivateRunSpanGamut(text);
        }

        private IEnumerable<Block> PrivateRunBlockGamut(string text, bool supportTextAlignment)
        {
            var index = 0;
            var length = text.Length;
            var rtn = new List<Block>();

            var candidates = new List<Candidate>();

            while (true)
            {
                candidates.Clear();

                foreach (var parser in ParseParam.PrimaryBlocks)
                {
                    var match = parser.FirstMatchPattern.Match(text, index, length);
                    if (match.Success) candidates.Add(new Candidate(match, parser));
                }

                if (candidates.Count == 0) break;

                candidates.Sort();

                int bestBegin = 0;
                int bestEnd = 0;
                IEnumerable<Block>? result = null;

                foreach (var c in candidates)
                {
                    result = c.Parser.Parse(text, c.Match, supportTextAlignment, this, out bestBegin, out bestEnd);
                    if (result is not null) break;
                }

                if (result is null) break;

                if (bestBegin > index)
                {
                    RunBlockRest(text, index, bestBegin - index, supportTextAlignment, ParseParam.SecondlyBlocks, 0, rtn);
                }

                rtn.AddRange(result);

                length -= bestEnd - index;
                index = bestEnd;
            }

            if (index < text.Length)
            {
                RunBlockRest(text, index, text.Length - index, supportTextAlignment, ParseParam.SecondlyBlocks, 0, rtn);
            }

            return rtn;
        }

        private void RunBlockRest(
            string text, int index, int length,
            bool supportTextAlignment,
            IBlockParser[] parsers, int parserStart,
            List<Block> outto)
        {
            for (; parserStart < parsers.Length; ++parserStart)
            {
                var parser = parsers[parserStart];

                for (; ; )
                {
                    var match = parser.FirstMatchPattern.Match(text, index, length);
                    if (!match.Success) break;

                    var rslt = parser.Parse(text, match, supportTextAlignment, this, out int parseBegin, out int parserEnd);
                    if (rslt is null) break;

                    if (parseBegin > index)
                    {
                        RunBlockRest(text, index, parseBegin - index, supportTextAlignment, parsers, parserStart + 1, outto);
                    }
                    outto.AddRange(rslt);

                    length -= parserEnd - index;
                    index = parserEnd;
                }

                if (length == 0) break;
            }

            if (length != 0)
            {
                outto.AddRange(FormParagraphs(text.Substring(index, length), supportTextAlignment));
            }
        }

        private IEnumerable<Inline> PrivateRunSpanGamut(string text)
        {
            var rtn = new List<Inline>();
            RunSpanRest(text, 0, text.Length, ParseParam.Inlines, 0, rtn);
            return rtn;
        }

        /// <summary>
        /// Perform transformations that occur *within* block-level tags like paragraphs, headers, and list items.
        /// </summary>
        private void RunSpanRest(
            string text, int index, int length,
            IInlineParser[] parsers, int parserStart,
            List<Inline> outto)
        {
            for (; parserStart < parsers.Length; ++parserStart)
            {
                var parser = parsers[parserStart];

                for (; ; )
                {
                    var match = parser.FirstMatchPattern.Match(text, index, length);
                    if (!match.Success) break;

                    var rslt = parser.Parse(text, match, this, out int parseBegin, out int parserEnd);
                    if (rslt is null) break;

                    if (parseBegin > index)
                    {
                        RunSpanRest(text, index, parseBegin - index, parsers, parserStart + 1, outto);
                    }
                    outto.AddRange(rslt);

                    length -= parserEnd - index;
                    index = parserEnd;
                }

                if (length == 0) break;
            }

            if (length != 0)
            {
                var subtext = text.Substring(index, length);

                outto.AddRange(
                    StrictBoldItalic ?
                        DoText(subtext) :
                        DoTextDecorations(subtext, s => DoText(s)));
            }
        }


        #region grammer - paragraph

        private static readonly Regex _align = new(@"^p([<=>])\.", RegexOptions.Compiled);
        private static readonly Regex _newlinesLeadingTrailing = new(@"^\n+|\n+\z", RegexOptions.Compiled);
        private static readonly Regex _newlinesMultiple = new(@"\n{2,}", RegexOptions.Compiled);

        /// <summary>
        /// splits on two or more newlines, to form "paragraphs";    
        /// </summary>
        private IEnumerable<Block> FormParagraphs(string text, bool supportTextAlignment)
        {
            var trimemdText = _newlinesLeadingTrailing.Replace(text, "");

            string[] grafs = trimemdText == "" ?
                EnumerableExt.Empty<string>() :
                _newlinesMultiple.Split(trimemdText);

            foreach (var g in grafs)
            {
                var chip = g;

                TextAlignment? indiAlignment = null;

                if (supportTextAlignment)
                {
                    var alignMatch = _align.Match(chip);
                    if (alignMatch.Success)
                    {
                        chip = chip.Substring(alignMatch.Length);
                        switch (alignMatch.Groups[1].Value)
                        {
                            case "<":
                                indiAlignment = TextAlignment.Left;
                                break;
                            case ">":
                                indiAlignment = TextAlignment.Right;
                                break;
                            case "=":
                                indiAlignment = TextAlignment.Center;
                                break;
                        }
                    }
                }

                var block = Create<Paragraph, Inline>(PrivateRunSpanGamut(chip));
                if (NormalParagraphStyle is not null)
                {
                    block.Style = NormalParagraphStyle;
                }
                if (indiAlignment.HasValue)
                {
                    block.TextAlignment = indiAlignment.Value;
                }

                yield return block;
            }
        }

        #endregion

        #region grammer - image or href

        /// <summary>
        /// Reusable pattern to match balanced [brackets]. See Friedl's 
        /// "Mastering Regular Expressions", 2nd Ed., pp. 328-331.
        /// </summary>
        private static readonly string _nestedBracketsPattern =
                    TextUtil.RepeatString(@"
                    (?>              # Atomic matching
                       [^\[\]]+      # Anything other than brackets
                     |
                       \[
                           ", _nestDepth)
                    + TextUtil.RepeatString(
                    @" \]
                    )*"
                    , _nestDepth);

        /// <summary>
        /// Reusable pattern to match balanced (parens). See Friedl's 
        /// "Mastering Regular Expressions", 2nd Ed., pp. 328-331.
        /// </summary>
        private static readonly string _nestedParensPattern =
                    TextUtil.RepeatString(@"
                    (?>            # Atomic matching
                       [^()\n\t]+? # Anything other than parens or whitespace
                     |
                       \(
                           ", _nestDepth)
                    + TextUtil.RepeatString(
                    @" \)
                    )*?"
                    , _nestDepth);

        private static readonly Regex _imageOrHrefInline = new(string.Format(@"
                (                           # wrap whole match in $1
                    (!)?                    # image maker = $2
                    \[
                        ({0})               # link text = $3
                    \]
                    \(                      # literal paren
                        [ ]*
                        ({1})               # href = $4
                        [ ]*
                        (                   # $5
                        (['""])             # quote char = $6
                        (.*?)               # title = $7
                        \6                  # matching quote
                        [ ]*                # ignore any spaces between closing quote and )
                        )?                  # title is optional
                    \)
                )", _nestedBracketsPattern, _nestedParensPattern),
                  RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex _resizeImage = new(string.Format(@"
                (                           # wrap whole match in $1
                    (!)                     # image maker = $2
                    \[
                        ({0})               # link text = $3
                    \]
                    \(                      # literal paren
                        [ ]*
                        ({1})               # href = $4
                        [ ]*
                        (                   # $5
                        (['""])             # quote char = $6
                        (.*?)               # title = $7
                        \6                  # matching quote
                        [ ]*                # ignore any spaces between closing quote and )
                        )?                  # title is optional
                    \)
                    \{{
                        ([^\}}]+)             # size = $8
                    \}}
                )", _nestedBracketsPattern, _nestedParensPattern),
                  RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex _stylePattern = new Regex(@"
                [ ]+
                (?<name>width|height)
                [ ]*
                =
                [ ]*
                (?<value>[0-9]*(\.[0-9]+)?) 
                (?<unit>(%|em|ex|mm|Q|cm|in|pt|pc|px|))
                ",
                RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private Inline ImageOrHrefInlineEvaluator(Match match)
        {
            if (String.IsNullOrEmpty(match.Groups[2].Value))
            {
                return TreatsAsHref(match);
            }
            else
            {
                return TreatsAsImage(match);
            }
        }

        private Inline TreatsAsHref(Match match)
        {
            string linkText = match.Groups[3].Value;
            string url = match.Groups[4].Value;
            string title = match.Groups[7].Value;

            var result = Create<Hyperlink, Inline>(PrivateRunSpanGamut(linkText));
            result.CommandParameter = url;
            result.Command = HyperlinkCommand;
            if (OnHyperLinkClicked is not null)
            {
                var holder = new CallbackHolder(OnHyperLinkClicked);
                result.Click += holder.Clicked;
            }

            if (!DisabledTootip)
            {
                result.ToolTip = string.IsNullOrWhiteSpace(title) ?
                    url :
                    String.Format("\"{0}\"\r\n{1}", title, url);
            }

            if (LinkStyle is not null)
            {
                result.Style = LinkStyle;
            }

            return result;
        }

        private Inline TreatsAsImage(Match match)
        {
            string linkText = match.Groups[3].Value;
            string urlTxt = match.Groups[4].Value;
            string title = match.Groups[7].Value;

            try
            {
                return LoadImage(linkText, urlTxt, title);
            }
            catch (Exception ex)
            {
                var errorRun = new Run($"Error: Can not load image from {urlTxt}, {ex.Message}");
                errorRun.Foreground = Brushes.Red;
                return errorRun;
            }
        }

        private Inline ImageWithSizeEvaluator(Match match)
        {
            string linkText = match.Groups[3].Value;
            string urlTxt = match.Groups[4].Value;
            string title = match.Groups[7].Value;
            string style = " " + match.Groups[8].Value;

            List<Action<FrameworkElement>> effects = new();
            foreach (var mch in _stylePattern.Matches(style).Cast<Match>())
            {
                if (!ImageIndicate.TryCreate(mch.Groups["value"].Value, mch.Groups["unit"].Value, out var indicate))
                    continue;

                effects.Add(mch.Groups["name"].Value == "width" ?
                    indicate.ApplyToWidth : indicate.ApplyToHeight);
            }

            InlineUIContainer image = LoadImage(linkText, urlTxt, title, (container, image, source) =>
            {
                if (container.Child is FrameworkElement element)
                    foreach (var effect in effects)
                        effect(element);
            });

            return image;
        }

        public InlineUIContainer LoadImage(
            string? tag, string urlTxt, string? tooltipTxt,
            Action<InlineUIContainer, Image?, ImageSource?>? onSuccess = null)
        {
            var urls = new List<Uri>();

            if (Uri.IsWellFormedUriString(urlTxt, UriKind.Absolute) || Path.IsPathRooted(urlTxt))
            {
                urls.Add(new Uri(urlTxt));
            }
            else
            {
                if (BaseUri is not null)
                {
                    urls.Add(new Uri(BaseUri, urlTxt));
                }
                if (AssetPathRoot is not null)
                {
                    if (Uri.IsWellFormedUriString(AssetPathRoot, UriKind.Absolute))
                    {
                        urls.Add(new Uri(new Uri(AssetPathRoot), urlTxt));
                    }
                    else if (Path.IsPathRooted(AssetPathRoot))
                    {
                        urls.Add(new Uri(Path.Combine(AssetPathRoot, urlTxt)));
                    }
                }
            }

            var container = new InlineUIContainer();
            var loading = new ImageLoading(this, tag, urlTxt, tooltipTxt, container, onSuccess);

            if (DisabledLazyLoad)
            {
                loading.Treats(LoaderManager.LoadImage(urls));
            }
            else
            {
                container.Child = new Label() { Content = $"Load {urlTxt}" };
                loading.Treats(LoaderManager.LoadImageAsync(urls));
            }

            return container;
        }


        #endregion


        #region grammer - header

        private static readonly Regex _headerSetext = new(@"
                ^(.+?)
                [ ]*
                \n
                (=+|-+)     # $1 = string of ='s or -'s
                [ ]*
                \n+",
                RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex _headerAtx = new(@"
                ^(\#{1,6})  # $1 = string of #'s
                [ ]*
                (.+?)       # $2 = Header text
                [ ]*
                \#*         # optional closing #'s (not counted)
                \n+",
                RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown headers into HTML header tags
        /// </summary>
        /// <remarks>
        /// Header 1  
        /// ========  
        /// 
        /// Header 2  
        /// --------  
        /// 
        /// # Header 1  
        /// ## Header 2  
        /// ## Header 2 with closing hashes ##  
        /// ...  
        /// ###### Header 6  
        /// </remarks>
        private Block SetextHeaderEvaluator(Match match)
        {
            string header = match.Groups[1].Value;
            int level = match.Groups[2].Value.StartsWith("=") ? 1 : 2;

            //TODO: Style the paragraph based on the header level
            return CreateHeader(level, PrivateRunSpanGamut(header.Trim()));
        }

        private Block AtxHeaderEvaluator(Match match)
        {
            string header = match.Groups[2].Value;
            int level = match.Groups[1].Value.Length;
            return CreateHeader(level, PrivateRunSpanGamut(header));
        }

        public Block CreateHeader(int level, IEnumerable<Inline> content)
        {
            var block = Create<Paragraph, Inline>(content);

            switch (level)
            {
                case 1:
                    if (Heading1Style is not null)
                    {
                        block.Style = Heading1Style;
                    }
                    if (!DisabledTag)
                    {
                        block.Tag = TagHeading1;
                    }
                    break;

                case 2:
                    if (Heading2Style is not null)
                    {
                        block.Style = Heading2Style;
                    }
                    if (!DisabledTag)
                    {
                        block.Tag = TagHeading2;
                    }
                    break;

                case 3:
                    if (Heading3Style is not null)
                    {
                        block.Style = Heading3Style;
                    }
                    if (!DisabledTag)
                    {
                        block.Tag = TagHeading3;
                    }
                    break;

                case 4:
                    if (Heading4Style is not null)
                    {
                        block.Style = Heading4Style;
                    }
                    if (!DisabledTag)
                    {
                        block.Tag = TagHeading4;
                    }
                    break;

                case 5:
                    if (Heading5Style is not null)
                    {
                        block.Style = Heading5Style;
                    }
                    if (!DisabledTag)
                    {
                        block.Tag = TagHeading5;
                    }
                    break;

                case 6:
                    if (Heading6Style is not null)
                    {
                        block.Style = Heading6Style;
                    }
                    if (!DisabledTag)
                    {
                        block.Tag = TagHeading6;
                    }
                    break;
            }

            return block;
        }
        #endregion

        #region grammer - Note
        private static readonly Regex _note = new(@"
                ^(\<)       # $1 = starting marker <
                [ ]*
                (.+?)       # $2 = Header text
                [ ]*
                \>*         # optional closing >'s (not counted)
                \n+
            ", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown into HTML paragraphs.
        /// </summary>
        /// <remarks>
        /// < Note
        /// </remarks>
        private Block NoteEvaluator(Match match, bool supportTextAlignment)
        {
            string text = match.Groups[2].Value;

            TextAlignment? indiAlignment = null;

            if (supportTextAlignment)
            {
                var alignMatch = _align.Match(text);
                if (alignMatch.Success)
                {
                    text = text.Substring(alignMatch.Length);
                    switch (alignMatch.Groups[1].Value)
                    {
                        case "<":
                            indiAlignment = TextAlignment.Left;
                            break;
                        case ">":
                            indiAlignment = TextAlignment.Right;
                            break;
                        case "=":
                            indiAlignment = TextAlignment.Center;
                            break;
                    }
                }
            }

            return NoteComment(PrivateRunSpanGamut(text), indiAlignment);
        }

        public Block NoteComment(IEnumerable<Inline> content, TextAlignment? indiAlignment)
        {
            var block = Create<Paragraph, Inline>(content);
            if (NoteStyle is not null)
            {
                block.Style = NoteStyle;
            }
            if (!DisabledTag)
            {
                block.Tag = TagNote;
            }
            if (indiAlignment.HasValue)
            {
                block.TextAlignment = indiAlignment.Value;
            }

            return block;
        }
        #endregion

        #region grammer - horizontal rules

        private static readonly Regex _horizontalRules = new(@"
                ^[ ]{0,3}                   # Leading space
                    ([-=*_])                # $1: First marker ([markers])
                    (?>                     # Repeated marker group
                        [ ]{0,2}            # Zero, one, or two spaces.
                        \1                  # Marker character
                    ){2,}                   # Group repeated at least twice
                    [ ]*                    # Trailing spaces
                    \n                      # End of line.
                ", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex _horizontalCommonRules = new(@"
                ^[ ]{0,3}                   # Leading space
                    ([-*_])                 # $1: First marker ([markers])
                    (?>                     # Repeated marker group
                        [ ]{0,2}            # Zero, one, or two spaces.
                        \1                  # Marker character
                    ){2,}                   # Group repeated at least twice
                    [ ]*                    # Trailing spaces
                    \n                      # End of line.
                ", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown horizontal rules into HTML hr tags
        /// </summary>
        /// <remarks>
        /// ***  
        /// * * *  
        /// ---
        /// - - -
        /// </remarks>
        private Block RuleEvaluator(Match match)
        {
            switch (match.Groups[1].Value)
            {
                default:
                case "-":
                    {
                        var sep = new Separator();
                        if (SeparatorStyle is not null)
                            sep.Style = SeparatorStyle;

                        var container = new BlockUIContainer(sep);
                        if (!DisabledTag)
                        {
                            container.Tag = TagRuleSingle;
                        }
                        return container;
                    }

                case "=":
                    {
                        var stackPanel = new StackPanel();
                        for (int i = 0; i < 2; i++)
                        {
                            var sep = new Separator();
                            if (SeparatorStyle is not null)
                                sep.Style = SeparatorStyle;

                            stackPanel.Children.Add(sep);
                        }

                        var container = new BlockUIContainer(stackPanel);
                        if (!DisabledTag)
                        {
                            container.Tag = TagRuleDouble;
                        }
                        return container;
                    }

                case "*":
                    {
                        var stackPanel = new StackPanel();
                        for (int i = 0; i < 2; i++)
                        {
                            var sep = new Separator()
                            {
                                Margin = new Thickness(0)
                            };

                            if (SeparatorStyle is not null)
                                sep.Style = SeparatorStyle;

                            stackPanel.Children.Add(sep);
                        }

                        var container = new BlockUIContainer(stackPanel);
                        if (!DisabledTag)
                        {
                            container.Tag = TagRuleBold;
                        }
                        return container;
                    }

                case "_":
                    {
                        var stackPanel = new StackPanel();
                        for (int i = 0; i < 2; i++)
                        {
                            var sep = new Separator()
                            {
                                Margin = new Thickness(0)
                            };

                            if (SeparatorStyle is not null)
                                sep.Style = SeparatorStyle;

                            stackPanel.Children.Add(sep);
                        }

                        var sepLst = new Separator();
                        if (SeparatorStyle is not null)
                            sepLst.Style = SeparatorStyle;

                        stackPanel.Children.Add(sepLst);

                        var container = new BlockUIContainer(stackPanel);
                        if (!DisabledTag)
                        {
                            container.Tag = TagRuleBoldWithSingle;
                        }
                        return container;
                    }
            }
        }

        private Block RuleCommonEvaluator(Match match)
        {
            var sep = new Separator();
            if (SeparatorStyle is not null)
                sep.Style = SeparatorStyle;

            var container = new BlockUIContainer(sep);
            if (!DisabledTag)
            {
                container.Tag = TagRuleSingle;
            }
            return container;
        }
        #endregion


        #region grammer - list

        // `alphabet order` and `roman number` must start 'a.'～'c.' and 'i,'～'iii,'.
        // This restrict is avoid to treat "Yes," as list marker.
        private const string _extFirstListMaker = @"(?:[*+=-]|\d+[.]|[a-c][.]|[i]{1,3}[,]|[A-C][.]|[I]{1,3}[,])";
        private const string _extSubseqListMaker = @"(?:[*+=-]|\d+[.]|[a-c][.]|[cdilmvx]+[,]|[A-C][.]|[CDILMVX]+[,])";

        private const string _commonListMaker = @"(?:[*+-]|\d+[.])";

        //private const string _markerUL = @"[*+=-]";
        //private const string _markerOL = @"\d+[.]|\p{L}+[.,]";

        // Unordered List
        private const string _markerUL_Disc = @"[*]";
        private const string _markerUL_Box = @"[+]";
        private const string _markerUL_Circle = @"[-]";
        private const string _markerUL_Square = @"[=]";

        private static readonly Regex _startsWith_markerUL_Disc = new("\\A" + _markerUL_Disc, RegexOptions.Compiled);
        private static readonly Regex _startsWith_markerUL_Box = new("\\A" + _markerUL_Box, RegexOptions.Compiled);
        private static readonly Regex _startsWith_markerUL_Circle = new("\\A" + _markerUL_Circle, RegexOptions.Compiled);
        private static readonly Regex _startsWith_markerUL_Square = new("\\A" + _markerUL_Square, RegexOptions.Compiled);

        // Ordered List
        private const string _markerOL_Number = @"\d+[.]";
        private const string _markerOL_LetterLower = @"[a-c][.]";
        private const string _markerOL_LetterUpper = @"[A-C][.]";
        private const string _markerOL_RomanLower = @"[cdilmvx]+[,]";
        private const string _markerOL_RomanUpper = @"[CDILMVX]+[,]";

        private static readonly Regex _startsWith_markerOL_Number = new("\\A" + _markerOL_Number, RegexOptions.Compiled);
        private static readonly Regex _startsWith_markerOL_LetterLower = new("\\A" + _markerOL_LetterLower, RegexOptions.Compiled);
        private static readonly Regex _startsWith_markerOL_LetterUpper = new("\\A" + _markerOL_LetterUpper, RegexOptions.Compiled);
        private static readonly Regex _startsWith_markerOL_RomanLower = new("\\A" + _markerOL_RomanLower, RegexOptions.Compiled);
        private static readonly Regex _startsWith_markerOL_RomanUpper = new("\\A" + _markerOL_RomanUpper, RegexOptions.Compiled);

        /// <summary>
        /// Maximum number of levels a single list can have.
        /// In other words, _listDepth - 1 is the maximum number of nested lists.
        /// </summary>
        private const int _listDepth = 4;

        private static readonly string _wholeListFormat = @"
            ^
            (?<whltxt>                      # whole list
              (?<mkr_i>                     # list marker with indent
                (?![ ]{{0,3}}(?<hrm>[-=*_])([ ]{{0,2}}\k<hrm>){{2,}}[ ]*\n)
                (?<idt>[ ]{{0,{2}}})
                (?<mkr>{0})                 # first list item marker
                [ ]+
              )
              (?s:.+?)
              (                             # $4
                  \z
                |
                  \n{{2,}}
                  (?=\S)
                  (?!                       # Negative lookahead for another list item marker
                    [ ]*
                    {1}[ ]+
                  )
              )
            )";

        private static readonly Regex _startNoIndentRule = new(@"\A[ ]{0,2}(?<hrm>[-=*_])([ ]{0,2}\k<hrm>){2,}[ ]*$",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex _startQuoteOrHeader = new(@"\A(\#{1,6}[ ]|>|```)", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);


        private static readonly Regex _startNoIndentCommonSublistMarker = new(@"\A" + _commonListMaker, RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex _commonListNested = new(
            String.Format(_wholeListFormat, _commonListMaker, _commonListMaker, _listDepth - 1),
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);


        private static readonly Regex _startNoIndentExtSublistMarker = new(@"\A" + _extSubseqListMaker, RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex _extListNested = new(
            String.Format(_wholeListFormat, _extFirstListMaker, _extSubseqListMaker, _listDepth - 1),
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);


        private IEnumerable<Block> ExtListEvaluator(Match match)
            => ListEvaluator(match, _startNoIndentExtSublistMarker);

        private IEnumerable<Block> CommonListEvaluator(Match match)
            => ListEvaluator(match, _startNoIndentCommonSublistMarker);

        private IEnumerable<Block> ListEvaluator(Match match, Regex sublistMarker)
        {
            // Check text marker style.
            var markerDetect = GetTextMarkerStyle(match.Groups["mkr"].Value);
            TextMarkerStyle textMarker = markerDetect.Item1;
            string markerPattern = markerDetect.Item2;
            Regex markerRegex = markerDetect.Item3;
            int indentAppending = markerDetect.Item4;

            // count indent from first marker with indent
            int countIndent = TextUtil.CountIndent(match.Groups["mkr_i"].Value);

            // whole list
            string[] whileListLins = match.Groups["whltxt"].Value.Split('\n');

            // collect detendentable line
            var listBulder = new StringBuilder();
            var outerListBuildre = new StringBuilder();
            var isInOuterList = false;
            foreach (var line in whileListLins)
            {
                if (!isInOuterList)
                {
                    if (String.IsNullOrEmpty(line))
                    {
                        listBulder.Append('\n');
                    }
                    else if (TextUtil.TryDetendLine(line, countIndent, out var stripedLine))
                    {
                        // is it horizontal line?
                        if (_startNoIndentRule.IsMatch(stripedLine))
                        {
                            isInOuterList = true;
                        }
                        // is it header or blockquote?
                        else if (_startQuoteOrHeader.IsMatch(stripedLine))
                        {
                            isInOuterList = true;
                        }
                        // is it had list marker?
                        else if (sublistMarker.IsMatch(stripedLine))
                        {
                            // is it same marker as now processed?
                            if (markerRegex.IsMatch(stripedLine))
                            {
                                listBulder.Append(stripedLine).Append('\n');
                            }
                            else isInOuterList = true;
                        }
                        else
                        {
                            var detentedline = TextUtil.DetentLineBestEffort(stripedLine, indentAppending);
                            listBulder.Append(detentedline).Append('\n');
                        }
                    }
                    else isInOuterList = true;
                }

                if (isInOuterList)
                {
                    outerListBuildre.Append(line).Append('\n');
                }
            }

            string list = listBulder.ToString();

            var resultList = Create<List, ListItem>(ProcessListItems(list, markerPattern));

            resultList.MarkerStyle = textMarker;

            yield return resultList;

            if (outerListBuildre.Length != 0)
            {
                foreach (var ctrl in PrivateRunBlockGamut(outerListBuildre.ToString(), ParseParam.SupportTextAlignment))
                    yield return ctrl;
            }
        }

        /// <summary>
        /// Process the contents of a single ordered or unordered list, splitting it
        /// into individual list items.
        /// </summary>
        private IEnumerable<ListItem> ProcessListItems(string list, string marker)
        {
            // The listLevel global keeps track of when we're inside a list.
            // Each time we enter a list, we increment it; when we leave a list,
            // we decrement. If it's zero, we're not in a list anymore.

            // We do this because when we're not inside a list, we want to treat
            // something like this:

            //    I recommend upgrading to version
            //    8. Oops, now this line is treated
            //    as a sub-list.

            // As a single paragraph, despite the fact that the second line starts
            // with a digit-period-space sequence.

            // Whereas when we're inside a list (or sub-list), that line will be
            // treated as the start of a sub-list. What a kludge, huh? This is
            // an aspect of Markdown's syntax that's hard to parse perfectly
            // without resorting to mind-reading. Perhaps the solution is to
            // change the syntax rules such that sub-lists must start with a
            // starting cardinal number; e.g. "1." or "a.".


            // Trim trailing blank lines:
            list = Regex.Replace(list, @"\n{2,}\z", "\n");

            string pattern = string.Format(
                @"(\n)?                  # leading line = $1
            (^[ ]*)                    # leading whitespace = $2
            ({0}) [ ]+                 # list marker = $3
            ((?s:.+?)                  # list item text = $4
            (\n{{1,2}}))      
            (?= \n* (\z | \2 ({0}) [ ]+))", marker);

            var regex = new Regex(pattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);
            var matches = regex.Matches(list);
            foreach (var m in matches.Cast<Match>())
            {
                yield return ListItemEvaluator(m);
            }
        }

        private ListItem ListItemEvaluator(Match match)
        {
            string item = match.Groups[4].Value;
            string leadingLine = match.Groups[1].Value;

            if (!String.IsNullOrEmpty(leadingLine) || Regex.IsMatch(item, @"\n{2,}"))
                // we could correct any bad indentation here..
                return Create<ListItem, Block>(PrivateRunBlockGamut(item, false));
            else
            {
                // recursion for sub-lists
                return Create<ListItem, Block>(PrivateRunBlockGamut(item, false));
            }
        }

        /// <summary>
        /// Get the text marker style based on a specific regex.
        /// </summary>
        /// <param name="markerText">list maker (eg. * + 1. a. </param>
        /// <returns>
        ///     1; return Type. 
        ///     2: match regex pattern
        ///     3: char length of listmaker
        /// </returns>
        private static Tuple<TextMarkerStyle, string, Regex, int> GetTextMarkerStyle(string markerText)
        {
            if (Regex.IsMatch(markerText, _markerUL_Disc))
            {
                return Tuple.Create(TextMarkerStyle.Disc, _markerUL_Disc, _startsWith_markerUL_Disc, 2);
            }
            else if (Regex.IsMatch(markerText, _markerUL_Box))
            {
                return Tuple.Create(TextMarkerStyle.Box, _markerUL_Box, _startsWith_markerUL_Box, 2);
            }
            else if (Regex.IsMatch(markerText, _markerUL_Circle))
            {
                return Tuple.Create(TextMarkerStyle.Circle, _markerUL_Circle, _startsWith_markerUL_Circle, 2);
            }
            else if (Regex.IsMatch(markerText, _markerUL_Square))
            {
                return Tuple.Create(TextMarkerStyle.Square, _markerUL_Square, _startsWith_markerUL_Square, 2);
            }
            else if (Regex.IsMatch(markerText, _markerOL_Number))
            {
                return Tuple.Create(TextMarkerStyle.Decimal, _markerOL_Number, _startsWith_markerOL_Number, 3);
            }
            else if (Regex.IsMatch(markerText, _markerOL_LetterLower))
            {
                return Tuple.Create(TextMarkerStyle.LowerLatin, _markerOL_LetterLower, _startsWith_markerOL_LetterLower, 3);
            }
            else if (Regex.IsMatch(markerText, _markerOL_LetterUpper))
            {
                return Tuple.Create(TextMarkerStyle.UpperLatin, _markerOL_LetterUpper, _startsWith_markerOL_LetterUpper, 3);
            }
            else if (Regex.IsMatch(markerText, _markerOL_RomanLower))
            {
                return Tuple.Create(TextMarkerStyle.LowerRoman, _markerOL_RomanLower, _startsWith_markerOL_RomanLower, 3);
            }
            else if (Regex.IsMatch(markerText, _markerOL_RomanUpper))
            {
                return Tuple.Create(TextMarkerStyle.UpperRoman, _markerOL_RomanUpper, _startsWith_markerOL_RomanUpper, 3);
            }

            throw new InvalidOperationException("sorry library manager forget to modify about listmerker.");
        }

        #endregion


        #region grammer - table

        private static readonly Regex _table = new(@"
            (                               # whole table
                [ \n]*
                (?<hdr>                     # table header
                    ([^\n\|]*\|[^\n]+)
                )
                [ ]*\n[ ]*
                (?<col>                     # column style
                    \|?([ ]*:?-+:?[ ]*(\||$))+
                )
                (?<row>                     # table row
                    (
                        [ ]*\n[ ]*
                        ([^\n\|]*\|[^\n]+)
                    )+
                )
            )",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        private Block TableEvalutor(Match match)
        {
            var headerTxt = match.Groups["hdr"].Value.Trim();
            var styleTxt = match.Groups["col"].Value.Trim();
            var rowTxt = match.Groups["row"].Value.Trim();

            static string ExtractCoverBar(string txt)
            {
                if (txt[0] == '|')
                    txt = txt.Substring(1);

                if (String.IsNullOrEmpty(txt))
                    return txt;

                if (txt[txt.Length - 1] == '|')
                    txt = txt.Substring(0, txt.Length - 1);

                return txt;
            }

            // split columns by '|' but ignore '\|'
            static string[] SplitColumns(string text)
            {
                text = ExtractCoverBar(text);

                text = text.Replace("\\|", "" + (char)5);

                var columns = text.Split('|').Select(x => x.Replace((char)5, '|'))
                    .ToArray();

                return columns;
            }

            var mdtable = new MdTable(
                SplitColumns(headerTxt),
                SplitColumns(styleTxt).Select(txt => txt.Trim()).ToArray(),
                rowTxt.Split('\n').Select(ritm =>
                {
                    var trimRitm = ritm.Trim();
                    return SplitColumns(trimRitm);
                }).ToList());

            // table
            var table = new Table();
            if (TableStyle is not null)
            {
                table.Style = TableStyle;
            }

            // table columns
            while (table.Columns.Count < mdtable.ColCount)
                table.Columns.Add(new TableColumn());

            // table header
            var tableHeaderRG = new TableRowGroup();
            if (TableHeaderStyle is not null)
            {
                tableHeaderRG.Style = TableHeaderStyle;
            }
            if (!DisabledTag)
            {
                tableHeaderRG.Tag = TagTableHeader;
            }

            var tableHeader = CreateTableRow(mdtable.Header);
            tableHeaderRG.Rows.Add(tableHeader);
            table.RowGroups.Add(tableHeaderRG);

            // row
            var tableBodyRG = new TableRowGroup();
            if (TableBodyStyle is not null)
            {
                tableBodyRG.Style = TableBodyStyle;
            }
            if (!DisabledTag)
            {
                tableBodyRG.Tag = TagTableBody;
            }

            foreach (int rowIdx in Enumerable.Range(0, mdtable.Details.Count))
            {
                var tableBody = CreateTableRow(mdtable.Details[rowIdx]);
                if (!DisabledTag)
                {
                    tableBody.Tag = (rowIdx & 1) == 0 ? TagOddTableRow : TagEvenTableRow;
                }

                tableBodyRG.Rows.Add(tableBody);
            }
            table.RowGroups.Add(tableBodyRG);

            return table;
        }

        private TableRow CreateTableRow(IList<MdTableCell> mdcells)
        {
            var tableRow = new TableRow();

            foreach (var mdcell in mdcells)
            {
                TableCell cell = mdcell.Text is null ?
                    new TableCell() :
                    new TableCell(Create<Paragraph, Inline>(PrivateRunSpanGamut(mdcell.Text)));

                if (mdcell.Horizontal.HasValue)
                    cell.TextAlignment = mdcell.Horizontal.Value;

                if (mdcell.RowSpan != 1)
                    cell.RowSpan = mdcell.RowSpan;

                if (mdcell.ColSpan != 1)
                    cell.ColumnSpan = mdcell.ColSpan;

                tableRow.Cells.Add(cell);
            }

            return tableRow;
        }

        #endregion


        #region grammer - code block

        private static readonly Regex _codeBlockFirst = new(@"
                    ^          # Character before opening
                    [ ]{0,3}
                    (`{3,})          # $1 = Opening run of `
                    ([^\n`]*)        # $2 = The code lang
                    \n
                    ((.|\n)+?)       # $3 = The code block
                    \n[ ]*
                    \1
                    (?!`)[\n]+", RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex _indentCodeBlock = new(@"
                    (?:\A|^[ ]*\n)
                    (
                    [ ]{4}.+
                    (\n([ ]{4}.+|[ ]*))*
                    \n?
                    )
                    ", RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);

        private Block CodeBlocksWithLangEvaluator(Match match)
            => CodeBlocksEvaluator(match.Groups[2].Value, match.Groups[3].Value);

        private Block CodeBlocksWithoutLangEvaluator(Match match)
        {
            var detentTxt = String.Join("\n", match.Groups[1].Value.Split('\n').Select(line => TextUtil.DetentLineBestEffort(line, 4)));
            return CodeBlocksEvaluator(null, _newlinesLeadingTrailing.Replace(detentTxt, ""));
        }

        private Block CodeBlocksEvaluator(string? lang, string code)
        {
            var txtEdit = new TextEditor();

            if (!String.IsNullOrEmpty(lang))
            {
                var highlight = ParseParam.HighlightManager.Get(lang);
                txtEdit.SetCurrentValue(TextEditor.SyntaxHighlightingProperty, highlight);
                txtEdit.Tag = lang;
            }

            txtEdit.Text = code;
            txtEdit.HorizontalAlignment = HorizontalAlignment.Stretch;
            txtEdit.IsReadOnly = true;
            txtEdit.PreviewMouseWheel += (s, e) =>
            {
                if (e.Handled) return;

                e.Handled = true;

                var isShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                if (isShiftDown)
                {
                    // horizontal scroll
                    var offset = txtEdit.HorizontalOffset;
                    offset -= e.Delta;
                    txtEdit.ScrollToHorizontalOffset(offset);
                }
                else
                {
                    // event bubbles
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                    {
                        RoutedEvent = UIElement.MouseWheelEvent,
                        Source = s,
                    };

                    var parentObj = ((Control)s).Parent;
                    if (parentObj is UIElement uielm)
                    {
                        uielm.RaiseEvent(eventArg);
                    }
                    else if (parentObj is ContentElement celem)
                    {
                        celem.RaiseEvent(eventArg);
                    }
                }
            };


            var result = new BlockUIContainer(txtEdit);
            if (!DisabledContextMenu)
            {
                CommandsForTextEditor.Setup(txtEdit);
            }
            if (CodeBlockStyle is not null)
            {
                result.Style = CodeBlockStyle;
            }
            if (!DisabledTag)
            {
                result.Tag = TagCodeBlock;
            }

            return result;
        }

        #endregion


        #region grammer - code

        //    * You can use multiple backticks as the delimiters if you want to
        //        include literal backticks in the code span. So, this input:
        //
        //        Just type ``foo `bar` baz`` at the prompt.
        //
        //        Will translate to:
        //
        //          <p>Just type <code>foo `bar` baz</code> at the prompt.</p>
        //
        //        There's no arbitrary limit to the number of backticks you
        //        can use as delimters. If you need three consecutive backticks
        //        in your code, use four for delimiters, etc.
        //
        //    * You can use spaces to get literal backticks at the edges:
        //
        //          ... type `` `bar` `` ...
        //
        //        Turns to:
        //
        //          ... type <code>`bar`</code> ...         
        //
        private static readonly Regex _codeSpan = new(@"
                    (?<!\\)   # Character before opening ` can't be a backslash
                    (`+)      # $1 = Opening run of `
                    (.+?)     # $2 = The code block
                    (?<!`)
                    \1
                    (?!`)", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown `code spans` into HTML code tags
        /// </summary>
        private Inline CodeSpanEvaluator(Match match)
        {
            string span = match.Groups[2].Value;
            span = Regex.Replace(span, @"^[ ]*", ""); // leading whitespace
            span = Regex.Replace(span, @"[ ]*$", ""); // trailing whitespace

            var result = new Run(span);
            if (CodeStyle is not null)
            {
                result.Style = CodeStyle;
            }
            if (!DisabledTag)
            {
                result.Tag = TagCode;
            }

            return result;
        }

        #endregion


        #region grammer - textdecorations

        private static readonly Regex _strictBold = new(@"([\W_]|^) (\*\*|__) (?=\S) ([^\r]*?\S[\*_]*) \2 ([\W_]|$)",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex _strictItalic = new(@"([\W_]|^) (\*|_) (?=\S) ([^\r\*_]*?\S) \2 ([\W_]|$)",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex _strikethrough = new(@"(~~) (?=\S) (.+?) (?<=\S) \1",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex _underline = new(@"(__) (?=\S) (.+?) (?<=\S) \1",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex _color = new(@"%\{[ \t]*color[ \t]*:([^\}]+)\}", RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown *italics* and **bold** into HTML strong and em tags
        /// </summary>
        private IEnumerable<Inline> DoTextDecorations(string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            var rtn = new List<Inline>();

            var buff = new StringBuilder();

            void HandleBefore()
            {
                if (buff.Length > 0)
                {
                    rtn.AddRange(defaultHandler(buff.ToString()));
                    buff.Clear();
                }
            }

            for (var i = 0; i < text.Length; ++i)
            {
                var ch = text[i];
                switch (ch)
                {
                    default:
                        buff.Append(ch);
                        break;

                    case '\\': // escape
                        if (++i < text.Length)
                        {
                            switch (text[i])
                            {
                                default:
                                    buff.Append('\\').Append(text[i]);
                                    break;

                                case '\\': // escape
                                case ':': // bold? or italic
                                case '*': // bold? or italic
                                case '~': // strikethrough?
                                case '_': // underline?
                                case '%': // color?
                                    buff.Append(text[i]);
                                    break;
                            }
                        }
                        else
                            buff.Append('\\');

                        break;

                    case ':': // emoji?
                        {
                            var nxtI = text.IndexOf(':', i + 1);
                            if (nxtI != -1 && EmojiTable.TryGet(text.Substring(i + 1, nxtI - i - 1), out var emoji))
                            {
                                buff.Append(emoji);
                                i = nxtI;
                            }
                            else buff.Append(':');
                            break;
                        }

                    case '*': // bold? or italic
                        {
                            var inline = ParseAsBoldOrItalic(text, ch, i, out int parseStart, out int parseEnd);

                            if (i < parseStart)
                            {
                                buff.Append(text, i, parseStart - i);
                            }

                            if (inline is not null)
                            {
                                HandleBefore();
                                rtn.Add(inline);
                            }

                            i = parseEnd - 1;
                            break;
                        }

                    case '~': // strikethrough?
                        if (ParseParam.SupportStrikethrough)
                        {
                            var oldI = i;
                            var inline = ParseAsStrikethrough(text, ref i);
                            if (inline is null)
                            {
                                buff.Append(text, oldI, i - oldI + 1);
                            }
                            else
                            {
                                HandleBefore();
                                rtn.Add(inline);
                            }
                            break;
                        }
                        else goto default;

                    case '_': // underline?
                        if (ParseParam.SupportTextileInline)
                        {
                            var oldI = i;
                            var inline = ParseAsUnderline(text, ref i);
                            if (inline is null)
                            {
                                buff.Append(text, oldI, i - oldI + 1);
                            }
                            else
                            {
                                HandleBefore();
                                rtn.Add(inline);
                            }
                            break;
                        }
                        else goto case '*';

                    case '%': // color?
                        if (ParseParam.SupportTextileInline)
                        {
                            var oldI = i;
                            var inline = ParseAsColor(text, ref i);
                            if (inline is null)
                            {
                                buff.Append(text, oldI, i - oldI + 1);
                            }
                            else
                            {
                                HandleBefore();
                                rtn.Add(inline);
                            }
                            break;
                        }
                        else goto default;
                }
            }

            if (buff.Length > 0)
            {
                rtn.AddRange(defaultHandler(buff.ToString()));
            }

            return rtn;
        }

        private Inline? ParseAsUnderline(string text, ref int start)
        {
            var bgnCnt = CountRepeat(text, start, '_');

            int last = EscapedIndexOf(text, start + bgnCnt, '_');

            int endCnt = last >= 0 ? CountRepeat(text, last, '_') : -1;

            if (endCnt >= 2 && bgnCnt >= 2)
            {
                int cnt = 2;
                int bgn = start + cnt;
                int end = last;

                start = end + cnt - 1;
                var span = Create<Underline, Inline>(PrivateRunSpanGamut(text.Substring(bgn, end - bgn)));
                if (!DisabledTag)
                {
                    span.Tag = TagUnderlineSpan;
                }
                return span;
            }
            else
            {
                start += bgnCnt - 1;
                return null;
            }
        }

        private Inline? ParseAsStrikethrough(string text, ref int start)
        {
            var bgnCnt = CountRepeat(text, start, '~');

            int last = EscapedIndexOf(text, start + bgnCnt, '~');

            int endCnt = last >= 0 ? CountRepeat(text, last, '~') : -1;

            if (endCnt >= 2 && bgnCnt >= 2)
            {
                int cnt = 2;
                int bgn = start + cnt;
                int end = last;

                start = end + cnt - 1;
                var span = Create<Span, Inline>(PrivateRunSpanGamut(text.Substring(bgn, end - bgn)));
                span.TextDecorations = TextDecorations.Strikethrough;

                if (!DisabledTag)
                {
                    span.Tag = TagStrikethroughSpan;
                }
                return span;
            }
            else
            {
                start += bgnCnt - 1;
                return null;
            }
        }

        private Inline? ParseAsBoldOrItalic(string text, char symbol, int start, out int parseStart, out int parseEnd)
        {
            bool isUnder = symbol == '_';
            int bgnCnt = CountRepeat(text, start, symbol);
            int bgnLft = bgnCnt;

            if (start > 0 && isUnder && !Char.IsWhiteSpace(text[start - 1]))
            {
                parseStart = start + bgnCnt;
                parseEnd = parseStart;
                return null;
            }

            int bgn = start + bgnCnt;
            parseStart = start;

            if (bgn < text.Length && Char.IsWhiteSpace(text[bgn]))
            {
                parseStart += bgnLft;
                parseEnd = bgn;
                return null;
            }

            var content = new List<Inline>();
            for (int i = bgn; i < text.Length;)
            {
                char c = text[i];

                if (c == '\\')
                {
                    i += 2;
                    continue;

                }
                else if (c == symbol)
                {
                    int endCnt = CountRepeat(text, i, symbol);

                    if (Char.IsWhiteSpace(text[i - 1]))
                    {
                        ParseAsBoldOrItalic(text, symbol, i, out int _, out int subEnd);
                        i = subEnd;
                        continue;
                    }

                    if (isUnder)
                    {
                        int check = i + endCnt;
                        if (check < text.Length && !Char.IsWhiteSpace(text[check]))
                        {
                            ++i;
                            continue;
                        }
                    }

                    content.AddRange(PrivateRunSpanGamut(text.Substring(bgn, i - bgn)));

                    var inline = Create(Math.Min(bgnLft, endCnt), content);
                    content.Clear();
                    content.Add(inline);

                    if (bgnLft > endCnt)
                    {
                        bgnLft -= endCnt;
                        i += endCnt;
                        bgn = i;
                        continue;
                    }
                    else if (bgnLft == endCnt)
                    {
                        parseEnd = i + endCnt;
                        return content[0];
                    }
                    else
                    {
                        parseEnd = i + bgnLft;
                        return content[0];
                    }
                }
                else ++i;
            }

            parseStart += bgnLft;
            parseEnd = bgn;
            return content.FirstOrDefault();


            Inline Create(int symbolCnt, IEnumerable<Inline> inlines)
            {
                if (symbolCnt == 1)
                {
                    var italic = Create<Italic, Inline>(inlines);
                    if (!DisabledTag)
                    {
                        italic.Tag = TagItalicSpan;
                    }
                    return italic;
                }
                else if (symbolCnt == 2)
                {
                    var bold = Create<Bold, Inline>(inlines);
                    if (!DisabledTag)
                    {
                        bold.Tag = TagBoldSpan;
                    }
                    return bold;
                }
                else
                {
                    var span = Create<Italic, Inline>(inlines);
                    var bold = new Bold(span);
                    if (!DisabledTag)
                    {
                        span.Tag = TagItalicSpan;
                        bold.Tag = TagBoldSpan;
                    }
                    return bold;
                }
            }
        }

        private Inline? ParseAsColor(string text, ref int start)
        {
            var mch = _color.Match(text, start);

            if (mch.Success && start == mch.Index)
            {
                int bgnIdx = start + mch.Value.Length;
                int endIdx = EscapedIndexOf(text, bgnIdx, '%');

                Span span;
                if (endIdx == -1)
                {
                    endIdx = text.Length - 1;
                    span = Create<Span, Inline>(
                        PrivateRunSpanGamut(text.Substring(bgnIdx)));
                }
                else
                {
                    span = Create<Span, Inline>(
                        PrivateRunSpanGamut(text.Substring(bgnIdx, endIdx - bgnIdx)));
                }

                var colorLbl = mch.Groups[1].Value.Trim();

                try
                {
                    var color = colorLbl.StartsWith("#") ?
                        (SolidColorBrush)new BrushConverter().ConvertFrom(colorLbl) :
                        (SolidColorBrush)new BrushConverter().ConvertFromString(colorLbl);

                    span.Foreground = color;
                }
                catch { }

                start = endIdx;
                return span;
            }
            else return null;
        }


        private static int EscapedIndexOf(string text, int start, char target)
        {
            for (var i = start; i < text.Length; ++i)
            {
                var ch = text[i];
                if (ch == '\\') ++i;
                else if (ch == target) return i;
            }
            return -1;
        }
        private static int CountRepeat(string text, int start, char target)
        {
            var count = 0;

            for (var i = start; i < text.Length; ++i)
            {
                if (text[i] == target) ++count;
                else break;
            }

            return count;
        }


        private Inline ItalicEvaluator(Match match)
        {
            var content = match.Groups[3].Value;
            var span = Create<Italic, Inline>(PrivateRunSpanGamut(content));
            if (!DisabledTag)
            {
                span.Tag = TagItalicSpan;
            }
            return span;
        }

        private Inline BoldEvaluator(Match match)
        {
            var content = match.Groups[3].Value;
            var span = Create<Bold, Inline>(PrivateRunSpanGamut(content));
            if (!DisabledTag)
            {
                span.Tag = TagBoldSpan;
            }
            return span;
        }

        private Inline StrikethroughEvaluator(Match match)
        {
            var content = match.Groups[2].Value;

            var span = Create<Span, Inline>(PrivateRunSpanGamut(content));
            span.TextDecorations = TextDecorations.Strikethrough;
            if (!DisabledTag)
            {
                span.Tag = TagStrikethroughSpan;
            }
            return span;
        }

        private Inline UnderlineEvaluator(Match match)
        {
            var content = match.Groups[2].Value;
            var span = Create<Underline, Inline>(PrivateRunSpanGamut(content));
            if (!DisabledTag)
            {
                span.Tag = TagUnderlineSpan;
            }
            return span;
        }

        #endregion


        #region grammer - text

        private static readonly Regex _eoln = new("\\s+");
        private static readonly Regex _lbrk = new(@"\ {2,}\n");

        public IEnumerable<Inline> DoText(string text)
        {
            text = UseSoftlineBreakAsHardlineBreak ? text.Replace("\n", "  \n") : text;
            var lines = _lbrk.Split(text);
            bool first = true;
            foreach (var line in lines)
            {
                if (first)
                    first = false;
                else
                    yield return new LineBreak();
                var t = _eoln.Replace(line, " ");
                yield return new Run(t);
            }
        }

        #endregion

        #region grammer - blockquote

        private static readonly Regex _blockquoteFirst = new(@"
            ^
            ([>].*)
            (\n[>].*)*
            [\n]*
            ", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private Section BlockquotesEvaluator(Match match)
        {
            // trim '>'
            var trimmedTxt = string.Join(
                    "\n",
                    match.Value.Trim().Split('\n')
                        .Select(txt =>
                        {
                            if (txt.Length <= 1) return string.Empty;
                            var trimmed = txt.Substring(1);
                            if (trimmed.FirstOrDefault() == ' ') trimmed = trimmed.Substring(1);
                            return trimmed;
                        })
                        .ToArray()
            );

            var blocks = PrivateRunBlockGamut(TextUtil.Normalize(trimmedTxt), ParseParam.SupportTextAlignment);
            var result = Create<Section, Block>(blocks);
            if (BlockquoteStyle is not null)
            {
                result.Style = BlockquoteStyle;
            }
            if (!DisabledTag)
            {
                result.Tag = TagBlockquote;
            }

            return result;
        }


        #endregion

        #region helper - parse

        private static TResult Create<TResult, TContent>(IEnumerable<TContent> content)
            where TResult : IAddChild, new()
        {
            var result = new TResult();
            foreach (var c in content)
            {
                result.AddChild(c);
            }

            return result;
        }

        #endregion
    }

    public delegate void HyperLinkClickCallback(string url);

    public class CallbackHolder
    {
        private HyperLinkClickCallback _callback;

        public CallbackHolder(HyperLinkClickCallback callback)
        {
            _callback = callback;
        }

        public void Clicked(object sender, RoutedEventArgs args)
        {
            var hyperlink = sender as Hyperlink;
            if (hyperlink is not null)
            {
                string? url = hyperlink.CommandParameter as string;
                _callback.Invoke(url!);
            }
        }
    }

    internal class ParseParam
    {
        public IBlockParser[] PrimaryBlocks { get; }
        public IBlockParser[] SecondlyBlocks { get; }
        public IInlineParser[] Inlines { get; }
        public bool SupportTextAlignment { get; }
        public bool SupportStrikethrough { set; get; }
        public bool SupportTextileInline { get; set; }
        public InternalHighlightManager HighlightManager { get; }

        public ParseParam(
            IEnumerable<IBlockParser> primary,
            IEnumerable<IBlockParser> secondly,
            IEnumerable<IInlineParser> inlines,
            SyntaxManager syntax,
            InternalHighlightManager highlightManager)
        {

            PrimaryBlocks = primary.ToArray();
            SecondlyBlocks = secondly.ToArray();
            Inlines = inlines.ToArray();
            SupportTextAlignment = syntax.EnableTextAlignment;
            SupportStrikethrough = syntax.EnableStrikethrough;
            SupportTextileInline = syntax.EnableTextileInline;
            HighlightManager = highlightManager;
        }
    }

    internal class ImageLoading
    {
        private readonly string? _tag;
        private readonly string _urlTxt;
        private readonly string? _tooltipTxt;
        private readonly InlineUIContainer _container;
        private readonly Action<InlineUIContainer, Image?, ImageSource?>? _onSuccess;

        private readonly Style? _imageStyle;

        public ImageLoading(
            Markdown owner,
            string? tag, string urlTxt, string? tooltipTxt,
            InlineUIContainer container,
            Action<InlineUIContainer, Image?, ImageSource?>? onSuccess)
        {
            _tag = tag;
            _urlTxt = urlTxt;
            _tooltipTxt = owner.DisabledTootip ? "" : tooltipTxt;
            _container = container;
            _onSuccess = onSuccess;

            _imageStyle = owner.ImageStyle;
        }

        public void Treats(Task<ImageLoaderManager.Result<FrameworkElement>> task)
        {
            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            dispatcher.Invoke(async () => Treats(await task));
        }

        public void Treats(ImageLoaderManager.Result<FrameworkElement> result)
        {
            var element = result.Value;

            if (element is null)
            {
                _container.Child = new Label()
                {
                    Foreground = Brushes.Red,
                    Content = "!" + _urlTxt + "\r\n" + result.ErrorMessage
                };
            }
            else
            {
                Setup(element);
            }
        }


        private void Setup(FrameworkElement element)
        {
            if (!string.IsNullOrWhiteSpace(_tag))
            {
                element.Tag = _tag;
            }

            if (!string.IsNullOrWhiteSpace(_tooltipTxt))
            {
                element.ToolTip = _tooltipTxt;
            }

            var image = element as Image;
            if (image is not null)
            {
                if (_imageStyle is null)
                {
                    image.Margin = new Thickness(0);
                }
                else
                {
                    image.Style = _imageStyle;
                }

                if (image.Source is BitmapSource bs)
                {
                    if (bs.IsDownloading)
                    {
                        Binding binding = new(nameof(BitmapImage.Width));
                        binding.Source = bs;
                        binding.Mode = BindingMode.OneWay;

                        BindingExpressionBase bindingExpression = BindingOperations.SetBinding(image, Image.WidthProperty, binding);
                        bs.DownloadCompleted += downloadCompletedHandler;

                        void downloadCompletedHandler(object? sender, EventArgs e)
                        {
                            bs.DownloadCompleted -= downloadCompletedHandler;
                            bs.Freeze();
                            bindingExpression.UpdateTarget();
                        }
                    }
                    else
                    {
                        image.Width = bs.Width;
                    }

                }
            }

            _container.Child = element;
            _onSuccess?.Invoke(_container, image, image?.Source);
        }
    }

    internal struct Candidate : IComparable<Candidate>
    {
        public Match Match { get; }
        public IBlockParser Parser { get; }

        public Candidate(Match result, IBlockParser parser)
        {
            Match = result;
            Parser = parser;
        }

        public int CompareTo(Candidate other)
            => Match.Index.CompareTo(other.Match.Index);
    }

    internal class ImageIndicate
    {
        public double Value { get; }
        public string Unit { get; }

        public ImageIndicate(double value, string unit)
        {
            Value = (Unit = unit) switch
            {
                "em" => value * 11,
                "ex" => value * 11 / 2,
                "Q" => value * 3.77952755905512 / 4,
                "mm" => value * 3.77952755905512,
                "cm" => value * 37.7952755905512,
                "in" => value * 96,
                "pt" => value * 1.33333333333333,
                "pc" => value * 16,
                "px" => value,
                _ => value
            };
        }

        public void ApplyToHeight(FrameworkElement image)
        {
            if (Unit == "%")
            {
                image.SetBinding(
                    Image.HeightProperty,
                    new Binding(nameof(Image.Width))
                    {
                        RelativeSource = new RelativeSource(RelativeSourceMode.Self),
                        Converter = new MultiplyConverter(Value / 100)
                    });
            }
            else
            {
                image.Height = Value;
            }
        }

        public void ApplyToWidth(FrameworkElement image)
        {
            if (Unit == "%")
            {
                var parent = image.Parent;

                for (; ; )
                {
                    if (parent is FrameworkElement element)
                    {
                        parent = element;
                        break;
                    }
                    else if (parent is FrameworkContentElement content)
                    {
                        parent = content.Parent;
                    }
                    else break;
                }

                if (parent is FlowDocumentScrollViewer)
                {
                    var binding = CreateMultiBindingForFlowDocumentScrollViewer();
                    binding.Converter = new MultiMultiplyConverter2(Value / 100);
                    image.SetBinding(Image.WidthProperty, binding);
                }
                else
                {
                    var binding = CreateBinding(nameof(FrameworkElement.ActualWidth), typeof(FrameworkElement));
                    binding.Converter = new MultiplyConverter(Value / 100);
                    image.SetBinding(Image.WidthProperty, binding);
                }
            }
            else
            {
                image.Width = Value;
            }
        }

        private MultiBinding CreateMultiBindingForFlowDocumentScrollViewer()
        {
            var binding = new MultiBinding();

            var totalWidth = CreateBinding(nameof(FlowDocumentScrollViewer.ActualWidth), typeof(FlowDocumentScrollViewer));
            var verticalBarVis = CreateBinding(nameof(FlowDocumentScrollViewer.VerticalScrollBarVisibility), typeof(FlowDocumentScrollViewer));

            binding.Bindings.Add(totalWidth);
            binding.Bindings.Add(verticalBarVis);

            return binding;
        }

        private static Binding CreateBinding(string propName, Type ancestorType)
        {
            return new Binding(propName)
            {
                RelativeSource = new RelativeSource()
                {
                    Mode = RelativeSourceMode.FindAncestor,
                    AncestorType = ancestorType,
                }
            };
        }

        public static bool TryCreate(string valueText, string unit, out ImageIndicate indicate)
        {
            if (double.TryParse(valueText, out var value))
            {
                indicate = new ImageIndicate(value, unit);
                return true;
            }
            else
            {
                indicate = default;
                return false;
            }
        }

        class MultiplyConverter : IValueConverter
        {
            public double Value { get; }

            public MultiplyConverter(double v)
            {
                Value = v;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return Value * (Double)value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return ((Double)value) / Value;
            }
        }
        class MultiMultiplyConverter2 : IMultiValueConverter
        {
            public double Value { get; }

            public MultiMultiplyConverter2(double v)
            {
                Value = v;
            }

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                var value = (double)values[0];
                var visibility = (ScrollBarVisibility)values[1];

                if (visibility == ScrollBarVisibility.Visible)
                {
                    return Value * (value - SystemParameters.VerticalScrollBarWidth);
                }
                else
                {
                    return Value * (Double)value;
                }
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}