using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;

#if MIG_FREE
namespace Markdown.Xaml
#else
namespace MdXaml
#endif
{
    public static class EmojiTable
    {
        private static ConcurrentDictionary<String, String>? s_keywords;

        static EmojiTable()
        {
            s_keywords = LoadTxt();
        }


        /*
            Workaround for Visual Studio Xaml Designer.
            When you open MarkdownStyle from Xaml Designer,
            A null error occurs. Perhaps static constructor is not executed.         
        */
        static ConcurrentDictionary<String, String> LoadTxt()
        {
#if MIG_FREE
            var resourceName = "Markdown.Xaml.EmojiTable.txt";
#else
            var resourceName = "MdXaml.EmojiTable.txt";
#endif
            var dic = new ConcurrentDictionary<string, string>();

            Assembly asm = Assembly.GetCallingAssembly();
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream is null)
                throw new InvalidOperationException($"fail to load '{resourceName}'");

            using var reader = new StreamReader(stream, true);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var elms = line.Split('\t');
                dic[elms[1]] = elms[0];
            }

            return dic;
        }

        public static bool TryGet(
            string keyword,
#if !NETFRAMEWORK
            [MaybeNullWhen(false)]
#endif
            out string emoji)
        {
            if (s_keywords is null) s_keywords = LoadTxt();
            return s_keywords.TryGetValue(keyword, out emoji);
        }

    }
}
