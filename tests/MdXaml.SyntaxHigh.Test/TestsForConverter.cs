#if !MIG_FREE
using ICSharpCode.AvalonEdit.Highlighting;
using MdXaml.SyntaxHigh.Ext;
using MdXaml.Ext;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Media;

namespace MdXaml.Test
{
    class TestsForConverter
    {
        [Test]
        public void TryConvert()
        {
            var colors = typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static)
                                       .Select(fld => fld.GetValue(null))
                                       .Cast<Color>()
                                       .ToArray();

            Assert.AreNotEqual(0, colors.Length);

            foreach (var def in HighlightingManager.Instance.HighlightingDefinitions)
            {
                foreach (var foreCol in colors)
                {
                    var wrapper = new HighlightWrapper(def, foreCol);

                    foreach (var brsh in wrapper.NamedHighlightingColors)
                    {
                        brsh.Foreground?.GetColor(null);
                    }

                    var wrapperType = wrapper.GetType();
                    var cvtFldInf = wrapperType.GetField("_converted", BindingFlags.NonPublic | BindingFlags.Instance);

                    var cvt = (Dictionary<HighlightingRuleSet, HighlightingRuleSet>)cvtFldInf.GetValue(wrapper);
                    foreach (var rule in cvt.Values)
                    {
                        Look(rule);
                    }
                }
            }
        }

        private void Look(HighlightingRuleSet ruleSet)
        {
            foreach (var span in ruleSet.Spans)
            {
                span.StartColor?.Foreground?.GetColor(null);
                span.SpanColor?.Foreground?.GetColor(null);
                span.EndColor?.Foreground?.GetColor(null);
            }

            foreach (var rule in ruleSet.Rules)
            {
                rule.Color?.Foreground?.GetColor(null);
            }
        }
    }
}
#endif