using ApprovalTests;
using ApprovalTests.Reporters;
using MdXaml.Plugins;
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
using System.Windows.Threading;


#if !MIG_FREE
namespace MdXaml.Test
#else
namespace Markdown.Xaml.Test
#endif
{
    [UseReporter(typeof(DiffReporter))]
    public class PlainTests
    {
        static PlainTests()
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


        public PlainTests()
        {
            PackUriHelper.Create(new Uri("http://example.com"));

            var asm = Assembly.GetExecutingAssembly();
            assetPath = Path.GetDirectoryName(asm.Location);
            baseUri = new Uri($"pack://application:,,,/{asm.GetName().Name};Component/");
        }

        public static Markdown CreateMarkdown()
        {
            var markdown = new Markdown();
            markdown.Plugins = new MdXamlPlugins(SyntaxManager.Plain);
            return markdown;
        }


        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenTest1_generatesExpectedResult()
        {
            var text = Utils.LoadText("Test1.md");
            var markdown = CreateMarkdown();
            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenTest2_generatesExpectedResult()
        {
            var text = Utils.LoadText("Test1.md");
            var markdown = CreateMarkdown();
            markdown.DisabledTag = true;
            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenBoldAndItalic_generatesExpectedResult()
        {
            var text = Utils.LoadText("BoldAndItalic2.md");
            var markdown = CreateMarkdown();

            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenLists1_generatesExpectedResult()
        {
            var text = Utils.LoadText("Lists1.md");
            var markdown = CreateMarkdown();
            markdown.DisabledContextMenu = true;
            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenLists2_generatesExpectedResult()
        {
            var text = Utils.LoadText("Lists2.md");
            var markdown = CreateMarkdown();
            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenTables1_generatesExpectedResult()
        {
            var text = Utils.LoadText("Tables.md");
            var markdown = CreateMarkdown();
            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenTables2_generatesExpectedResult()
        {
            var text = Utils.LoadText("Tables.md");
            var markdown = CreateMarkdown();
            markdown.DisabledTag = true;
            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenHorizontalRules_generatesExpectedResult()
        {
            var text = Utils.LoadText("HorizontalRules.md");
            var markdown = CreateMarkdown();
            markdown.DisabledContextMenu = true;
            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenLinksInline1_generatesExpectedResult()
        {
            var text = Utils.LoadText("Links_inline_style.md");
            var markdown = CreateMarkdown();
            markdown.BaseUri = baseUri;
            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenLinksInline2_generatesExpectedResult()
        {
            var text = Utils.LoadText("Links_inline_style.md");
            var markdown = CreateMarkdown();
            markdown.BaseUri = baseUri;
            markdown.DisabledTootip = true;
            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenTextStyles_generatesExpectedResult()
        {
            var text = Utils.LoadText("Text_style.md");
            var markdown = CreateMarkdown();
            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenImages1_generatesExpectedResult()
        {
            var text = Utils.LoadText("Images.md");
            var markdown = CreateMarkdown();
            markdown.AssetPathRoot = assetPath;
            markdown.BaseUri = baseUri;

            var result = markdown.Transform(text);

            var resultXaml = Utils.AsXaml(result);

            var assetUri = new Uri(assetPath);

            // change absolute filepath to relative-like
            resultXaml = resultXaml.Replace("UriSource=\"" + assetPath, "UriSource=\"<assetpathroot>");
            resultXaml = resultXaml.Replace("Source=\"" + assetUri.AbsoluteUri, "Source=\"<assetpathrooturi>");

            Approvals.Verify(resultXaml);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenImages2_generatesExpectedResult()
        {
            var text = Utils.LoadText("Images.md");
            var markdown = CreateMarkdown();
            markdown.DisabledLazyLoad = true;
            markdown.AssetPathRoot = assetPath;
            markdown.BaseUri = baseUri;

            var result = markdown.Transform(text);
            var resultXaml = Utils.AsXaml(result);

            var assetUri = new Uri(assetPath);

            // change absolute filepath to relative-like
            resultXaml = resultXaml.Replace("UriSource=\"" + assetPath, "UriSource=\"<assetpathroot>");
            resultXaml = resultXaml.Replace("Source=\"" + assetUri.AbsoluteUri, "Source=\"<assetpathrooturi>");

            Approvals.Verify(resultXaml);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenImages3_generatesExpectedResult()
        {
            var text = Utils.LoadText("Images.md");
            var markdown = CreateMarkdown();
            markdown.DisabledTootip = true;
            markdown.AssetPathRoot = assetPath;
            markdown.BaseUri = baseUri;

            var result = markdown.Transform(text);
            var resultXaml = Utils.AsXaml(result);

            var assetUri = new Uri(assetPath);

            // change absolute filepath to relative-like
            resultXaml = resultXaml.Replace("UriSource=\"" + assetPath, "UriSource=\"<assetpathroot>");
            resultXaml = resultXaml.Replace("Source=\"" + assetUri.AbsoluteUri, "Source=\"<assetpathrooturi>");

            Approvals.Verify(resultXaml);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenBlockqoute_generatesExpectedResult()
        {
            var text = Utils.LoadText("Blockquite.md");
            var markdown = CreateMarkdown();

            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenMixing_generatesExpectedResult()
        {
            var text = Utils.LoadText("Mixing.md");
            var markdown = CreateMarkdown();
            markdown.AssetPathRoot = assetPath;
            markdown.BaseUri = baseUri;
            markdown.DisabledContextMenu = true;

            var result = markdown.Transform(text);
            var resultXaml = Utils.AsXaml(result);

            var assetUri = new Uri(assetPath);

            // change absolute filepath to relative-like
            resultXaml = resultXaml.Replace("UriSource=\"" + assetPath, "UriSource=\"<assetpathroot>");

            Approvals.Verify(resultXaml);
        }


        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenstring()
        {
            var resources = (ResourceDictionary)XamlReader.Parse(Utils.LoadText("IndentTest.xaml"));

            var markdownViewer = new MarkdownScrollViewer()
            {
                MarkdownStyle = null,
                Plugins = new MdXamlPlugins(SyntaxManager.Plain)
            };

            foreach (var idx in Enumerable.Range(1, 4))
            {
                var jaggingMarkdown = (string)resources["Indent" + idx];
                markdownViewer.HereMarkdown = jaggingMarkdown;
                var document = markdownViewer.Document;
                Approvals.Verify(Utils.AsXaml(document));
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenCodes_generatesExpectedResult()
        {
            var text = Utils.LoadText("Codes.md");
            var markdown = CreateMarkdown();
            markdown.AssetPathRoot = assetPath;
            markdown.DisabledContextMenu = true;

            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Transform_givenEmoji()
        {
            var text = Utils.LoadText("Emoji.md");
            var markdown = CreateMarkdown();
            markdown.AssetPathRoot = assetPath;
            markdown.BaseUri = baseUri;

            var result = markdown.Transform(text);
            Approvals.Verify(Utils.AsXaml(result));
        }
    }
}