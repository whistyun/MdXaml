using MdXaml.Plugins;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MdXaml
{
    internal class ImageLoaderManager
    {
        private static readonly HttpClient s_client = new();

        private readonly IDictionary<Uri, WeakReference<BitmapImage>> _resultCache
            = new ConcurrentDictionary<Uri, WeakReference<BitmapImage>>();

        private readonly List<IImageLoader> _loaders = new();

        public void Register(IImageLoader l) => _loaders.Add(l);

        public void Restructure(MdXamlPlugins plugins)
        {
            _loaders.Clear();
            _loaders.AddRange(plugins.ImageLoader);
        }

        public Result<BitmapImage> LoadImage(IEnumerable<Uri> resourceUrls)
        {
            Result<BitmapImage>? firsterr = null;

            foreach (var resourceUrl in resourceUrls)
            {
                var resourceResult = CheckResource(resourceUrl);
                if (resourceResult is not null)
                {
                    return resourceResult;
                }

                var stream = OpenStreamAsync(resourceUrl).Result;
                if (stream.Value is null)
                {
                    firsterr ??= new Result<BitmapImage>(stream.ErrorMessage);
                    continue;
                }

                var img = OpenImageDirect(stream.Value);
                if (img is null)
                {
                    firsterr ??= new Result<BitmapImage>("unsupported image format");
                    continue;
                }

                if (resourceUrl.Scheme == "http" || resourceUrl.Scheme == "https")
                {
                    _resultCache[resourceUrl] = new WeakReference<BitmapImage>(img);
                }

                return new Result<BitmapImage>(img);
            }

            return firsterr ?? throw new ArgumentException("no resourceUrls");
        }

        public async Task<Result<BitmapImage>> LoadImageAsync(IEnumerable<Uri> resourceUrls)
        {
            Result<BitmapImage>? firsterr = null;

            foreach (var resourceUrl in resourceUrls)
            {
                var resourceResult = CheckResource(resourceUrl);
                if (resourceResult is not null)
                {
                    return resourceResult;
                }

                var stream = await OpenStreamAsync(resourceUrl);
                if (stream.Value is null)
                {
                    firsterr ??= new Result<BitmapImage>(stream.ErrorMessage);
                    continue;
                }

                var img = await OpenImageOnUITrhead(stream.Value);
                if (img is null)
                {
                    firsterr ??= new Result<BitmapImage>("unsupported image format");
                    continue;
                }

                if (resourceUrl.Scheme == "http" || resourceUrl.Scheme == "https")
                {
                    _resultCache[resourceUrl] = new WeakReference<BitmapImage>(img);
                }

                return new Result<BitmapImage>(img);
            }

            return firsterr ?? throw new ArgumentException("no resourceUrls");
        }

        private Result<BitmapImage>? CheckResource(Uri resourceUrl)
        {
            if ((resourceUrl.Scheme == "http" || resourceUrl.Scheme == "https")
                && _resultCache.TryGetValue(resourceUrl, out var reference))
            {
                if (reference.TryGetTarget(out var cachedimg))
                {
                    return new Result<BitmapImage>(cachedimg);
                }
                _resultCache.Remove(resourceUrl);
            }
            return null;
        }

        private Task<BitmapImage?> OpenImageOnUITrhead(Stream stream)
        {
            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            return dispatcher.InvokeAsync(() => OpenImageDirect(stream))
                             .Task;
        }

        private BitmapImage? OpenImageDirect(Stream stream)
        {
            stream.Position = 0;

            try
            {
                var imgSource = new BitmapImage();
                imgSource.BeginInit();
                // close the stream after the BitmapImage is created
                imgSource.CacheOption = BitmapCacheOption.OnLoad;
                imgSource.StreamSource = stream;
                imgSource.EndInit();

                stream.Close();

                return imgSource;
            }
            catch { }

            foreach (var ld in _loaders)
            {
                try
                {
                    stream.Position = 0;
                    var img = ld.Load(stream);
                    if (img is not null)
                    {
                        stream.Close();
                        return img;
                    }
                }
                catch { }
            }

            stream.Close();
            return null;
        }

        private static async Task<Result<Stream>> OpenStreamAsync(Uri resourceUrl)
        {
            switch (resourceUrl.Scheme)
            {
                case "http":
                case "https":
                    var httpResult = await s_client.GetAsync(resourceUrl);
                    if (httpResult.StatusCode == HttpStatusCode.OK)
                    {
                        var webstream = await httpResult.Content.ReadAsStreamAsync();
                        var stream = await AsMemoryStream(webstream);

                        return new Result<Stream>(stream);
                    }

                    var content = await httpResult.Content.ReadAsStringAsync();

                    return new Result<Stream>($"{httpResult.StatusCode}: {content}");

                case "file":
                    if (File.Exists(resourceUrl.LocalPath))
                    {
                        var fileStream = File.OpenRead(resourceUrl.LocalPath);
                        var stream = await CheckSupportSeek(fileStream);
                        return new Result<Stream>(stream);
                    }

                    return new Result<Stream>("file not found");

                case "pack":
                    try
                    {
                        var packStream = Application.GetResourceStream(resourceUrl);
                        var stream = await CheckSupportSeek(packStream.Stream);
                        return new Result<Stream>(stream);
                    }
                    catch
                    {
                        return new Result<Stream>("resource not found");
                    }
            }

            return new Result<Stream>("unsupport scheme");


            static async Task<Stream> CheckSupportSeek(Stream stream)
            {
                if (stream.CanSeek)
                    return stream;

                return await AsMemoryStream(stream);
            }

            static async Task<MemoryStream> AsMemoryStream(Stream stream)
            {
                var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                stream.Close();

                return ms;
            }
        }

        public class Result<T> where T : class
        {
            public string ErrorMessage { get; }
            public T? Value { get; }

            public Result(T value)
            {
                ErrorMessage = "";
                Value = value;
            }

            public Result(string message)
            {
                ErrorMessage = message;
                Value = null;
            }
        }
    }
}
