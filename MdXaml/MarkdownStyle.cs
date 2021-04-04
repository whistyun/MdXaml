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
        static MarkdownStyle()
        {
            LoadXaml();
        }

        /*
            Workaround for Visual Studio Xaml Designer.
            When you open MarkdownStyle from Xaml Designer,
            A null error occurs. Perhaps static constructor is not executed.         
        */
        static void LoadXaml()
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
            ResourceDictionary resources = (ResourceDictionary)Application.LoadComponent(resourceUri);
            _standard = (Style)resources["DocumentStyleStandard"];
            _compact = (Style)resources["DocumentStyleCompact"];
            _githublike = (Style)resources["DocumentStyleGithubLike"];
            _sasabune = (Style)resources["DocumentStyleSasabune"];
            _sasabuneStandard = (Style)resources["DocumentStyleSasabuneStandard"];
            _sasabuneCompact = (Style)resources["DocumentStyleSasabuneCompact"];
        }

        private static Style _standard;
        private static Style _compact;
        private static Style _githublike;
        private static Style _sasabune;
        private static Style _sasabuneCompact;
        private static Style _sasabuneStandard;

        public static Style Standard
        {
            get
            {
                if (_standard == null) LoadXaml();
                return _standard;
            }
        }

        public static Style Compact
        {
            get
            {
                if (_compact == null) LoadXaml();
                return _compact;
            }
        }

        public static Style GithubLike
        {
            get
            {
                if (_githublike == null) LoadXaml();
                return _githublike;
            }
        }

        public static Style Sasabune
        {
            get
            {
                if (_sasabune == null) LoadXaml();
                return _sasabune;
            }
        }

        public static Style SasabuneStandard
        {
            get
            {
                if (_sasabuneStandard == null) LoadXaml();
                return _sasabuneStandard;
            }
        }

        public static Style SasabuneCompact
        {
            get
            {
                if (_sasabuneCompact == null) LoadXaml();
                return _sasabuneCompact;
            }
        }
    }
}
