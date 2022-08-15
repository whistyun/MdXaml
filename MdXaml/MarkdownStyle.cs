using System;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;

#if MIG_FREE
namespace Markdown.Xaml
#else
namespace MdXaml
#endif
{
    public static class MarkdownStyle
    {
        private const string DocumentStyleStandard = "DocumentStyleStandard";
        private const string DocumentStyleCompact = "DocumentStyleCompact";
        private const string DocumentStyleGithubLike = "DocumentStyleGithubLike";
        private const string DocumentStyleSasabune = "DocumentStyleSasabune";
        private const string DocumentStyleSasabuneStandard = "DocumentStyleSasabuneStandard";
        private const string DocumentStyleSasabuneCompact = "DocumentStyleSasabuneCompact";

        static MarkdownStyle()
        {
            var resources = LoadDictionary();
            _standard = (Style)resources[DocumentStyleStandard];
            _compact = (Style)resources[DocumentStyleCompact];
            _githublike = (Style)resources[DocumentStyleGithubLike];
            _sasabune = (Style)resources[DocumentStyleSasabune];
            _sasabuneStandard = (Style)resources[DocumentStyleSasabuneStandard];
            _sasabuneCompact = (Style)resources[DocumentStyleSasabuneCompact];
        }

        static ResourceDictionary LoadDictionary()
        {
#if MIG_FREE
            var resourceName = "/Markdown.Xaml;component/MarkdownMigFree.Style.xaml";
#else
            /*
                Workaround for XamlParseException.
                When you don't load 'ICSharpCode.AvalonEdit.dll',
                XamlReader fail to read xmlns:avalonEdit="http://icsharpcode.net..."
             */
            var txtedit = typeof(ICSharpCode.AvalonEdit.TextEditor);
            txtedit.ToString();

            var resourceName = "/MdXaml;component/Markdown.Style.xaml";
#endif

            var resourceUri = new Uri(resourceName, UriKind.RelativeOrAbsolute);
            return (ResourceDictionary)Application.LoadComponent(resourceUri);
        }

        /*
            Workaround for Visual Studio Xaml Designer.
            When you open MarkdownStyle from Xaml Designer,
            A null error occurs. Perhaps static constructor is not executed.         
        */
        static Style LoadXaml(string name)
        {
            return (Style)LoadDictionary()[name];
        }

        private static readonly Style _standard;
        private static readonly Style _compact;
        private static readonly Style _githublike;
        private static readonly Style _sasabune;
        private static readonly Style _sasabuneCompact;
        private static readonly Style _sasabuneStandard;

        public static Style Standard => _standard is null ? LoadXaml(DocumentStyleStandard) : _standard;
        public static Style Compact => _compact is null ? LoadXaml(DocumentStyleCompact) : _compact;
        public static Style GithubLike => _githublike is null ? LoadXaml(DocumentStyleGithubLike) : _githublike;
        public static Style Sasabune => _sasabune is null ? LoadXaml(DocumentStyleSasabune) : _sasabune;
        public static Style SasabuneStandard => _sasabuneStandard is null ? LoadXaml(DocumentStyleSasabuneStandard) : _sasabuneStandard;
        public static Style SasabuneCompact => _sasabuneCompact is null ? LoadXaml(DocumentStyleSasabuneCompact) : _sasabuneCompact;
    }
}
