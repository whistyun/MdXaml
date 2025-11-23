using ApprovalTests;
using ApprovalTests.Reporters;
using MdXamlTest;
using NUnit.Framework;
using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Markup;


namespace MixingTest
{
    [UseReporter(typeof(DiffReporter))]
    public class Tests
    {
        static Tests()
        {
            var fwNm = Utils.GetRuntimeName();
            Approvals.RegisterDefaultNamerCreation(() => new ChangeOutputPathNamer("Out/" + fwNm));
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
        public void MdXaml_Transform_givenMixing_generatesExpectedResult()
        {
            var text = Utils.LoadText("Mixing.md");
            var markdown = new MdXaml.Markdown()
            {
                AssetPathRoot = assetPath,
                BaseUri = baseUri,
                DisabledContextMenu = true,
            };
            if (markdown.Plugins.CodeBlockLoader.Count == 0)
            {
                markdown.Plugins.CodeBlockLoader.Add(new MdXaml.SyntaxHigh.AvalonCodeBlockLoader());
            }

            var result = markdown.Transform(text);
            var resultXaml = Utils.AsXaml(result);

            var assetUri = new Uri(assetPath);

            // change absolute filepath to relative-like
            resultXaml = resultXaml.Replace("UriSource=\"" + assetPath, "UriSource=\"<assetpathroot>");

            Approvals.Verify(resultXaml);
        }
    }
}