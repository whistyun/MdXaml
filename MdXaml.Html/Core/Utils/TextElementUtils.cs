using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace MdXaml.Html.Core.Utils
{
    internal class TextElementUtils
    {
        public static TResult Create<TResult, TContent>(IEnumerable<TContent> content)
            where TResult : IAddChild, new()
        {
            var result = new TResult();
            foreach (var c in content)
            {
                result.AddChild(c);
            }

            return result;
        }
    }
}
