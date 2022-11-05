#if !OnGitHubAction
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Threading;

namespace VisualTest
{
    public class Class1
    {
        private string _assemblyDirectory;
        private string _projectDirectory;
        private string _exeFilePath;

        private string _assetPath;

        private Process _process;
        private IntPtr _hwnd;

        public Class1()
        {
            var assemblyLocation = Assembly.GetCallingAssembly().Location;
            _assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            _projectDirectory = Path.GetDirectoryName(Path.GetDirectoryName(_assemblyDirectory));


            var relPath = _assemblyDirectory.Substring(_projectDirectory.Length + 1);

            _assetPath = Path.Combine(_projectDirectory, relPath, "Assets");
            _exeFilePath = Path.Combine(_projectDirectory.Replace("VisualTest", "VisualTestApp"), relPath, "VisualTestApp.exe");
        }

        [SetUp]
        public void Setup()
        {
            _process = Process.Start(_exeFilePath);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (_hwnd == IntPtr.Zero)
            {
                Thread.Sleep(1000);
                _hwnd = _process.MainWindowHandle;

                if (stopwatch.ElapsedMilliseconds > 5000)
                    throw new InvalidOperationException("Application startup timeout");
            }
        }


        [Test]
        public void CheckFileRemove()
        {
            var mainWindow = AutomationElement.FromHandle(_hwnd);

            var assetPathValPtn = mainWindow.FindPatternById<ValuePattern>("AssetPathRootTextBox");
            assetPathValPtn.SetValue(_assetPath);

            var markdownValPtn = mainWindow.FindPatternById<ValuePattern>("MarkdownPathTextBlox");
            markdownValPtn.SetValue("Markdown.txt");

            // wait for viewing markdown
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var markdownView = mainWindow.FindPatternById<TextPattern>("MarkdownScrollViewer");
            while (markdownView.DocumentRange.GetChildren().Length < 4)
            {
                Thread.Sleep(1000);

                if (stopwatch.ElapsedMilliseconds > 5000)
                    throw new InvalidOperationException("Markdown drawing timeout");
            }

            Thread.Sleep(1000);

            foreach (var file in Directory.GetFiles(_assetPath))
            {
                try
                {
                    TryRemove(file, 1000);
                }
                catch (Exception e)
                {
                    Assert.Fail($"remove failed: {Path.GetFileName(file)}: {e.Message}");
                }
            }
        }

        private void TryRemove(string path, long waitTime)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            for (; ; )
            {
                try
                {
                    File.Delete(path);
                    return;
                }
                catch
                {
                    if (stopwatch.ElapsedMilliseconds < waitTime)
                    {
                        Thread.Sleep(100);
                    }
                    else throw;
                }
            }
        }


        [TearDown]
        public void Closing()
        {
            _process.Kill();
        }
    }
}
#endif