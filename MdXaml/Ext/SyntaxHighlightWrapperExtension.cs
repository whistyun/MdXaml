#if ! MIG_FREE

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace MdXaml.Ext
{
    /// <summary>
    /// Change syntax color according to the Foreground color.
    /// </summary>
    /// <remarks>
    /// This class change hue and saturation of the syntax color according to Foreground.
    /// This class assume that Foreground is the complementary color of Background.
    /// 
    /// You may think It's better to change it according to Bachground,
    /// But Background may be declared as absolutly transparent.
    /// </remarks>
    class SyntaxHighlightWrapperExtension : MarkupExtension
    {
        /// <summary>
        /// The source of Foreground.
        /// </summary>
        public Type? TargetType { set; get; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var foreColor = new Binding(nameof(TextEditor.Foreground))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = TargetType }
            };
            var language = new Binding(nameof(TextEditor.Tag))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.Self)
            };

            var binding = new MultiBinding();
            binding.Mode = BindingMode.OneWay;
            binding.Bindings.Add(foreColor);
            binding.Bindings.Add(language);
            binding.Converter = new SyntaxHighlightWrapperConverter();

            return binding;
        }

        class SyntaxHighlightWrapperConverter : IMultiValueConverter
        {
            public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                string? codeLang = values[1] is string l ? l : null;

                if (String.IsNullOrEmpty(codeLang))
                    return null;

                var highlight = HighlightingManager.Instance.GetDefinitionByExtension("." + codeLang);
                if (highlight is null) return null;

                Color foreColor = values[0] is SolidColorBrush cBrush ? cBrush.Color : Colors.Black;

                try
                {
                    return new HighlightWrapper(highlight, foreColor);
                }
                catch (Exception e)
                {
                    Trace.Fail(e.ToString());
                    return highlight;
                }
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }

    public class HighlightWrapper : IHighlightingDefinition
    {
        IHighlightingDefinition baseDef;
        Color foreColor;

        private Dictionary<HighlightingRuleSet, HighlightingRuleSet> _converted;
        private Dictionary<string, HighlightingRuleSet> _namedRuleSet;
        private Dictionary<string, HighlightingColor> _namedColors;

        public HighlightWrapper(IHighlightingDefinition baseDef, Color foreColor)
        {
            this.baseDef = baseDef;
            this.foreColor = foreColor;

            _converted = new Dictionary<HighlightingRuleSet, HighlightingRuleSet>();
            _namedRuleSet = new Dictionary<string, HighlightingRuleSet>();
            _namedColors = new Dictionary<string, HighlightingColor>();

            foreach (var color in baseDef.NamedHighlightingColors)
            {
                var name = color.Name;

                var newCol = color.Clone();
                newCol.Foreground = color.Foreground is null ?
                    null : new MixHighlightingBrush(color.Foreground, foreColor);
                _namedColors[name] = newCol;
            }

            MainRuleSet = Wrap(baseDef.MainRuleSet);
        }

        public string Name => "Re:" + baseDef.Name;
        public HighlightingRuleSet? MainRuleSet { get; }
        public IEnumerable<HighlightingColor> NamedHighlightingColors => _namedColors.Values;
        public IDictionary<string, string> Properties => baseDef.Properties;

        public HighlightingColor? GetNamedColor(string name)
        {
            return _namedColors.TryGetValue(name, out var color) ? color : null;
        }

        public HighlightingRuleSet? GetNamedRuleSet(string name)
        {
            return _namedRuleSet.TryGetValue(name, out var rset) ? rset : null;
        }

#if !NETFRAMEWORK
        [return: NotNullIfNotNull("ruleSet")]
#endif
        private HighlightingRuleSet? Wrap(HighlightingRuleSet? ruleSet)
        {
            if (ruleSet is null) return null;

            if (!String.IsNullOrEmpty(ruleSet.Name)
                && _namedRuleSet.TryGetValue(ruleSet.Name, out var cachedRule))
                return cachedRule;

            if (_converted.TryGetValue(ruleSet, out var cachedRule2))
                return cachedRule2;

            var copySet = new HighlightingRuleSet();
            copySet.Name = ruleSet.Name;

            _converted[ruleSet] = copySet;
            if (!String.IsNullOrEmpty(copySet.Name))
                _namedRuleSet[copySet.Name] = copySet;

            foreach (var baseSpan in ruleSet.Spans)
            {
                if (baseSpan is null) continue;

                var copySpan = new HighlightingSpan();
                copySpan.StartExpression = baseSpan.StartExpression;
                copySpan.EndExpression = baseSpan.EndExpression;
                copySpan.RuleSet = Wrap(baseSpan.RuleSet);
                copySpan.StartColor = Wrap(baseSpan.StartColor);
                copySpan.SpanColor = Wrap(baseSpan.SpanColor);
                copySpan.EndColor = Wrap(baseSpan.EndColor);
                copySpan.SpanColorIncludesStart = baseSpan.SpanColorIncludesStart;
                copySpan.SpanColorIncludesEnd = baseSpan.SpanColorIncludesEnd;

                copySet.Spans.Add(copySpan);
            }

            foreach (var baseRule in ruleSet.Rules)
            {
                var copyRule = new HighlightingRule();
                copyRule.Regex = baseRule.Regex;
                copyRule.Color = Wrap(baseRule.Color);

                copySet.Rules.Add(copyRule);
            }

            return copySet;
        }

        private HighlightingColor? Wrap(HighlightingColor? color)
        {
            if (color is null) return null;

            if (!String.IsNullOrEmpty(color.Name)
                && _namedColors.TryGetValue(color.Name, out var cachedColor))
                return cachedColor;

            var copyColor = color.Clone();
            copyColor.Foreground = color.Foreground is null ?
                null : new MixHighlightingBrush(color.Foreground, foreColor);

            if (!String.IsNullOrEmpty(copyColor.Name))
                _namedColors[copyColor.Name] = copyColor;

            return copyColor;
        }
    }

    class MixHighlightingBrush : HighlightingBrush
    {
        HighlightingBrush baseBrush;
        Color fore;

        public MixHighlightingBrush(HighlightingBrush baseBrush, Color fore)
        {
            this.baseBrush = baseBrush;
            this.fore = fore;
        }

        public override Brush GetBrush(ITextRunConstructionContext context)
        {
            var color = GetColor(context);

            return color.HasValue ?
                new SolidColorBrush(color.Value) :
                GetBrush(context);
        }

        public override Color? GetColor(ITextRunConstructionContext context)
        {
            var colorN = this.baseBrush.GetColor(context);

            if (!colorN.HasValue) return colorN;

            var color = colorN.Value;

            if (color.A == 0) return colorN;

            return color.Brightness(fore);
        }
    }
}
#endif