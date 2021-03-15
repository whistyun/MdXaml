#if ! MIG_FREE

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace MdXaml.Ext
{
    public class SyntaxHighlightWrapperExtension : MarkupExtension
    {
        public Type TargetType { set; get; }

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
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                Color foreColor;
                if (values[0] is SolidColorBrush cBrush)
                {
                    foreColor = cBrush.Color;
                }
                else
                {
                    foreColor = Colors.Black;
                }

                string language;
                if (values[1] is string l)
                {
                    language = l;
                }
                else
                {
                    language = null;
                }

                if (!String.IsNullOrEmpty(language))
                {
                    var highlight = HighlightingManager.Instance.GetDefinitionByExtension("." + language);
                    if (highlight is null) return null;

                    return new HighlightWrapper(highlight, foreColor);
                }
                else return null;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        class HighlightWrapper : IHighlightingDefinition
        {
            IHighlightingDefinition baseDef;
            Color foreColor;

            public HighlightWrapper(IHighlightingDefinition baseDef, Color foreColor)
            {
                this.baseDef = baseDef;
                this.foreColor = foreColor;

                MainRuleSet = Mix(baseDef.MainRuleSet, foreColor);
                NamedHighlightingColors = baseDef.NamedHighlightingColors.Select(s => Mix(s, foreColor));
            }

            public string Name => baseDef.Name + ":Re";

            public HighlightingRuleSet MainRuleSet { get; }

            public IEnumerable<HighlightingColor> NamedHighlightingColors { get; }

            public IDictionary<string, string> Properties => baseDef.Properties;

            public HighlightingColor GetNamedColor(string name) => Mix(baseDef.GetNamedColor(name), foreColor);

            public HighlightingRuleSet GetNamedRuleSet(string name) => Mix(baseDef.GetNamedRuleSet(name), foreColor);
        }

        static HighlightingRule Mix(HighlightingRule baseRule, Color foreColor)
        {
            if (baseRule is null) return null;

            var copy = new HighlightingRule();
            copy.Regex = baseRule.Regex;
            copy.Color = Mix(baseRule.Color, foreColor);
            return copy;
        }

        static HighlightingSpan Mix(HighlightingSpan baseSpan, Color foreColor)
        {
            if (baseSpan is null) return null;

            var copy = new HighlightingSpan();
            copy.StartExpression = baseSpan.StartExpression;
            copy.EndExpression = baseSpan.EndExpression;
            copy.RuleSet = Mix(baseSpan.RuleSet, foreColor);
            copy.StartColor = Mix(baseSpan.StartColor, foreColor);
            copy.SpanColor = Mix(baseSpan.SpanColor, foreColor);
            copy.EndColor = Mix(baseSpan.EndColor, foreColor);
            copy.SpanColorIncludesStart = baseSpan.SpanColorIncludesStart;
            copy.SpanColorIncludesEnd = baseSpan.SpanColorIncludesEnd;

            return copy;
        }

        static HighlightingRuleSet Mix(HighlightingRuleSet ruleSet, Color fore)
        {
            if (ruleSet is null) return null;

            var copy = new HighlightingRuleSet();
            copy.Name = ruleSet.Name;

            foreach (var nspn in ruleSet.Spans.Select(spn => Mix(spn, fore)).ToArray())
                copy.Spans.Add(nspn);

            foreach (var nrule in ruleSet.Rules.Select(rule => Mix(rule, fore)).ToArray())
                copy.Rules.Add(nrule);

            return copy;
        }

        static HighlightingColor Mix(HighlightingColor color, Color fore)
        {
            if (color is null) return null;

            var copy = color.Clone();
            copy.Foreground = new MixHighlightingBrush(color.Foreground, fore);
            return copy;
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
                return new SolidColorBrush(GetColor(context).Value);
            }

            public override Color? GetColor(ITextRunConstructionContext context)
            {
                var colorN = this.baseBrush.GetColor(context);

                if (!colorN.HasValue) return fore;

                var color = colorN.Value;

                if (color.A == 0) return colorN;

                var reverseR = 0x7F < fore.R;
                var reverseG = 0x7F < fore.G;
                var reverseB = 0x7F < fore.B;

                byte BalanceColor(bool reverse, byte @base, byte adding)
                    => reverse ?
                            (byte)(@base - adding * @base / 255) :
                            (byte)(@base + adding * (255 - @base) / 255);

                if (colorN.HasValue)
                {
                    return (colorN.Value.A == 0) ? colorN :
                        Color.FromArgb(
                            colorN.Value.A,
                            BalanceColor(reverseR, fore.R, colorN.Value.R),
                            BalanceColor(reverseG, fore.G, colorN.Value.G),
                            BalanceColor(reverseB, fore.B, colorN.Value.B));
                }
                else return fore;
            }
        }
    }
}
#endif