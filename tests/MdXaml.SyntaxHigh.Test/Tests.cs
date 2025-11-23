using ApprovalTests;
using ApprovalTests.Reporters;
using MdXaml.Plugins;
using MdXamlTest;
using NUnit.Framework;
using System;
using System.IO;
using System.IO.Packaging;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;


#if !MIG_FREE
namespace MdXaml.Test
#else
namespace Markdown.Xaml.Test
#endif
{
    [UseReporter(typeof(DiffReporter))]
    public class Tests
    {
        static Tests()
        {
            var fwNm = Utils.GetRuntimeName();
#if !MIG_FREE

            Approvals.RegisterDefaultNamerCreation(() => new ChangeOutputPathNamer("Out/" + fwNm));
#else
            Approvals.RegisterDefaultNamerCreation(() => new ChangeOutputPathNamer("OutMF/"+ fwNm));
#endif
        }

        readonly string assetPath;
        readonly Uri baseUri;


        public Tests()
        {
            PackUriHelper.Create(new Uri("http://example.com"));

            var asm = Assembly.GetExecutingAssembly();
            assetPath = Path.GetDirectoryName(asm.Location);
            baseUri = new Uri($"pack://application:,,,/{asm.GetName().Name};Component/");
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenLists1_generatesExpectedResult()
        {
            var text = Utils.LoadText("Lists1.md");
            var markdown = new Markdown();
            markdown.Plugins.CodeBlockLoader.Add(new MdXaml.SyntaxHigh.AvalonCodeBlockLoader());
            markdown.DisabledContextMenu = true;
            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenHorizontalRules_generatesExpectedResult()
        {
            var text = Utils.LoadText("HorizontalRules.md");
            var markdown = new Markdown()
            {
                DisabledContextMenu = true,
            };
            markdown.Plugins.CodeBlockLoader.Add(new MdXaml.SyntaxHigh.AvalonCodeBlockLoader());
            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenMixing_generatesExpectedResult()
        {
            var text = Utils.LoadText("Mixing.md");
            var markdown = new Markdown()
            {
                AssetPathRoot = assetPath,
                BaseUri = baseUri,
                DisabledContextMenu = true,
            };
            markdown.Plugins.CodeBlockLoader.Add(new MdXaml.SyntaxHigh.AvalonCodeBlockLoader());

            var result = markdown.Transform(text);
            var resultXaml = Utils.AsXaml(result);

            var assetUri = new Uri(assetPath);

            // change absolute filepath to relative-like
            resultXaml = resultXaml.Replace("UriSource=\"" + assetPath, "UriSource=\"<assetpathroot>");

            Approvals.Verify(resultXaml);
        }


        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenCodes_generatesExpectedResult()
        {
            var text = Utils.LoadText("Codes.md");
            var markdown = new Markdown()
            {
                AssetPathRoot = assetPath,
                DisabledContextMenu = true,
            };
            markdown.Plugins.CodeBlockLoader.Add(new MdXaml.SyntaxHigh.AvalonCodeBlockLoader());

            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_MultipleCodeBlockLoader_generatesExpectedResult()
        {
            var text = Utils.LoadText("Codes.md");
            var markdown = new Markdown()
            {
                AssetPathRoot = assetPath,
                DisabledContextMenu = true,
            };
            markdown.Plugins.CodeBlockLoader.Add(new MdXaml.SyntaxHigh.AvalonCodeBlockLoader());
            markdown.Plugins.CodeBlockLoader.Add(new CodeButtonCopy());

            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        internal class CodeButtonCopy : ICodeBlockLoader
        {
            public FrameworkElement CodeBlocksEvaluator(string lang, string code, bool disabledContextMenu)
            {
                return new Button() { Content = "Copy" };
            }

            public void Register(Definition definition)
            {
                // TODO nothing
            }
        }

    }
}