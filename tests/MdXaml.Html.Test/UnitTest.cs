
using ApprovalTests;
using ApprovalTests.Reporters;
using MdXaml.Plugins;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Windows.Documents;

namespace MdXaml.Html.Test
{
    [UseReporter(typeof(DiffReporter))]
    public class UnitTest
    {
        private Markdown manager;

        [SetUp]
        [Apartment(ApartmentState.STA)]
        public void Setup()
        {
            var plugins = new MdXamlPlugins();
            plugins.Setups.Add(new HtmlPluginSetup());

            manager = new()
            {
                Plugins = plugins
            };
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Button()
        {
            var html = Utils.ReadHtml();

            var doc = manager.Transform(html);

            var xaml = Utils.AsXaml(doc);

            Approvals.Verify(xaml);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void CodeBlock()
        {
            var html = Utils.ReadHtml();

            var doc = manager.Transform(html);

            var xaml = Utils.AsXaml(doc);

            Approvals.Verify(xaml);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void InlineCode()
        {
            var html = Utils.ReadHtml();

            var doc = manager.Transform(html);

            var xaml = Utils.AsXaml(doc);

            Approvals.Verify(xaml);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Input()
        {
            var html = Utils.ReadHtml();

            var doc = manager.Transform(html);

            var xaml = Utils.AsXaml(doc);

            Approvals.Verify(xaml);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void List()
        {
            var html = Utils.ReadHtml();

            var doc = manager.Transform(html);

            var xaml = Utils.AsXaml(doc);

            Approvals.Verify(xaml);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Progres()
        {
            var html = Utils.ReadHtml();

            var doc = manager.Transform(html);

            var xaml = Utils.AsXaml(doc);

            Approvals.Verify(xaml);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void TypicalBlock()
        {
            var html = Utils.ReadHtml();

            var doc = manager.Transform(html);

            var xaml = Utils.AsXaml(doc);

            Approvals.Verify(xaml);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void TypicalInline()
        {
            var html = Utils.ReadHtml();

            var doc = manager.Transform(html);

            var xaml = Utils.AsXaml(doc);

            Approvals.Verify(xaml);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Mixing()
        {
            var html = Utils.ReadHtml();

            var doc = manager.Transform(html);

            var xaml = Utils.AsXaml(doc);

            Approvals.Verify(xaml);
        }
    }
}
