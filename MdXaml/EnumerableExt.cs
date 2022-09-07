using System;
using System.Collections.Generic;
using System.Text;

#if MIG_FREE
namespace Markdown.Xaml
#else
namespace MdXaml
#endif
{
    internal static class EnumerableExt
    {
        public static T[] Empty<T>() => EmptyArray<T>.Value;
    }

    internal class EmptyArray<T>
    {
        // net45 dosen't have Array.Empty<T>()
#pragma warning disable CA1825
        public static T[] Value = new T[0];
#pragma warning restore CA1825
    }
}
