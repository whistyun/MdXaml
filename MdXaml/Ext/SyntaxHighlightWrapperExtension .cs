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

                var foreMax = Math.Max(fore.R, Math.Max(fore.G, fore.B));
                var tgtHsv = new HSV(color);

                int newValue = tgtHsv.Value + foreMax;
                int newSaturation = tgtHsv.Saturation;
                if (newValue > 255)
                {
                    var newSaturation2 = newSaturation - (newValue - 255);
                    newValue = 255;

                    var sChRtLm = (color.R >= color.G && color.R >= color.B) ? 0.95f * 0.7f :
                                  (color.G >= color.R && color.G >= color.B) ? 0.95f :
                                                                               0.95f * 0.5f;

                    var sChRt = Math.Max(sChRtLm, newSaturation2 / (float)newSaturation);
                    newSaturation = (int)(newSaturation * sChRt);
                }

                tgtHsv.Value = (byte)newValue;
                tgtHsv.Saturation = (byte)newSaturation;

                var newColor = tgtHsv.ToColor();
                return newColor;
            }
        }

        struct HSV
        {
            public int Hue;
            public byte Saturation;
            public byte Value;

            public HSV(Color color)
            {
                int max = Math.Max(color.R, Math.Max(color.G, color.B));
                int min = Math.Min(color.R, Math.Min(color.G, color.B));
                int div = max - min;

                if (div == 0)
                {
                    Hue = 0;
                    Saturation = 0;
                }
                else
                {
                    Hue =
                            (min == color.B) ? 60 * (color.G - color.R) / div + 60 :
                            (min == color.R) ? 60 * (color.B - color.G) / div + 180 :
                                               60 * (color.R - color.B) / div + 300;
                    Saturation = (byte)div;
                }

                Value = (byte)max;
            }

            public Color ToColor()
            {
                if (Hue == 0 && Saturation == 0)
                {
                    return Color.FromRgb(Value, Value, Value);
                }

                //byte c = Saturation;

                int HueInt = Hue / 60;

                int x = (int)(Saturation * (1 - Math.Abs((Hue / 60f) % 2 - 1)));

                Color FromRgb(int r, int g, int b)
                    => Color.FromRgb((byte)r, (byte)g, (byte)b);


                switch (Hue / 60)
                {
                    default:
                    case 0: return FromRgb(Value, Value - Saturation + x, Value - Saturation);
                    case 1: return FromRgb(Value - Saturation + x, Value, Value - Saturation);
                    case 2: return FromRgb(Value - Saturation, Value, Value - Saturation + x);
                    case 3: return FromRgb(Value - Saturation, Value - Saturation + x, Value);
                    case 4: return FromRgb(Value - Saturation + x, Value - Saturation, Value);
                    case 5:
                    case 6: return FromRgb(Value, Value - Saturation, Value - Saturation + x);
                }
            }
        }

    }
}
#endif