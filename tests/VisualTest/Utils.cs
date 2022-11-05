using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows.Automation;

namespace VisualTest
{
    internal static class Utils
    {
        public static AutomationElement FindById(this AutomationElement root, string automationId)
            => root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));

        public static T FindPatternById<T>(this AutomationElement root, string automationId)
            where T : BasePattern
        {
            var element = FindById(root, automationId);
            var pattern = (AutomationPattern)typeof(T).GetField("Pattern").GetValue(null);

            return (T)element.GetCurrentPattern(pattern);
        }

    }
}
