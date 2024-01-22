using MdXaml.Plugins;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MdXaml
{
    internal class ImageLoaderManager
    {
        private static readonly HttpClient s_client = new();

        private readonly IDictionary<Uri, WeakReference<BitmapImage>> _resultCache
            = new ConcurrentDictionary<Uri, WeakReference<BitmapImage>>();

        private readonly List<IImageLoader> _iloaders = new();
        private readonly List<IElementLoader> _eloaders = new();

        public void Restructure(MdXamlPlugins plugins)
        {
            _iloaders.Clear();
            _eloaders.Clear();
            _iloaders.AddRange(plugins.ImageLoader);
            _eloaders.AddRange(plugins.ElementLoader);
        }

        public Result<FrameworkElement> LoadImage(IEnumerable<Uri> resourceUrls)
        {
            return PrivateLoadImageAsync(resourceUrls, false).Result;
        }

        public Task<Result<FrameworkElement>> LoadImageAsync(IEnumerable<Uri> resourceUrls)
        {
            return PrivateLoadImageAsync(resourceUrls, true);
        }

        public async Task<Result<FrameworkElement>> PrivateLoadImageAsync(IEnumerable<Uri> resourceUrls, bool dispatch)
        {
            Result<FrameworkElement>? firsterr = null;

            foreach (var resourceUrl in resourceUrls)
            {
                var cachedResult = CheckResource(resourceUrl);
                if (cachedResult is not null)
                {
                    var task = Create(cachedResult, dispatch);

                    return dispatch ? await task : task.Result;
                }

                var streamTask = OpenStreamAsync(resourceUrl);
                var streamResult = dispatch ? await streamTask : streamTask.Result;
                if (streamResult.Value is null)
                {
                    firsterr ??= new Result<FrameworkElement>(streamResult.ErrorMessage);
                    continue;
                }

                using var stream = streamResult.Value;
                var imageTask = OpenImage(stream, resourceUrl, dispatch);
                var imageResult = dispatch ? await imageTask : imageTask.Result;
                if (imageResult is null)
                {
                    firsterr ??= new Result<FrameworkElement>("unsupported image format");
                    continue;
                }

                return new Result<FrameworkElement>(imageResult);
            }

            return firsterr ?? throw new ArgumentException("no resourceUrls");
        }

        private BitmapImage? CheckResource(Uri resourceUrl)
        {
            if ((resourceUrl.Scheme == "http" || resourceUrl.Scheme == "https")
                && _resultCache.TryGetValue(resourceUrl, out var reference))
            {
                if (reference.TryGetTarget(out var cachedimg))
                {
                    return cachedimg;
                }
                _resultCache.Remove(resourceUrl);
            }
            return null;
        }

        private static async Task<Result<Stream>> OpenStreamAsync(Uri resourceUrl)
        {
            switch (resourceUrl.Scheme)
            {
                case "http":
                case "https":
                    var httpResult = await s_client.GetAsync(resourceUrl).ConfigureAwait(false);
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
                case "data":
                    try
                    {
                        Regex dataRegex = new(@"data:image/(?:png|jpg|jpeg);base64,(?<data>[^\n|[]*)");
                        
                        var match = dataRegex.Match(resourceUrl.AbsoluteUri);
                        if(!match.Success)
                            return new Result<Stream>("invalid format for scheme 'data'");
                        
                        var imageBytes = Convert.FromBase64String(match.Groups["data"].Value);

                        return new Result<Stream>(new MemoryStream(imageBytes, false));
                    }
                    catch
                    {
                        return new Result<Stream>("invalid data");
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

        private Task<FrameworkElement?> OpenImage(Stream stream, Uri? cacheKey, bool dispatch)
        {
            if (dispatch)
            {
                var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
                var operation = dispatcher.InvokeAsync(() => OpenImageDirect(stream));

                return operation.Task;
            }
            else
            {
                return Task.FromResult(OpenImageDirect(stream));
            }

            FrameworkElement? OpenImageDirect(Stream stream)
            {
                foreach (var ld in _iloaders.Where(ld => ld is IPreferredLoader))
                {
                    stream.Position = 0;

                    try
                    {
                        var img = ld.Load(stream);
                        if (img is not null)
                            return Create(cacheKey, img);
                    }
                    catch { }
                }

                foreach (var ld in _eloaders.Where(ld => ld is IPreferredLoader))
                {
                    stream.Position = 0;

                    try
                    {
                        var img = ld.Load(stream);
                        if (img is not null)
                            return img;
                    }
                    catch { }
                }

                stream.Position = 0;

                try
                {
                    var imgSource = new BitmapImage();
                    imgSource.BeginInit();
                    // close the stream after the BitmapImage is created
                    imgSource.CacheOption = BitmapCacheOption.OnLoad;
                    imgSource.StreamSource = stream;
                    imgSource.EndInit();

                    return Create(cacheKey, imgSource);
                }
                catch { }

                foreach (var ld in _iloaders.Where(ld => ld is not IPreferredLoader))
                {
                    stream.Position = 0;

                    try
                    {
                        var img = ld.Load(stream);
                        if (img is not null)
                            return Create(cacheKey, img);
                    }
                    catch { }
                }

                foreach (var ld in _eloaders.Where(ld => ld is not IPreferredLoader))
                {
                    stream.Position = 0;

                    try
                    {
                        var img = ld.Load(stream);
                        if (img is not null)
                            return img;
                    }
                    catch { }
                }

                return null;
            }

            FrameworkElement Create(Uri resourceKey, BitmapImage bitmap)
            {
                if (resourceKey.Scheme == "http" || resourceKey.Scheme == "https")
                {
                    _resultCache[resourceKey] = new WeakReference<BitmapImage>(bitmap);
                }

                return new Image { Source = bitmap };
            }
        }

        private Task<Result<FrameworkElement>> Create(BitmapImage image, bool dispatch)
        {
            if (dispatch)
            {
                var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
                var operation = dispatcher.InvokeAsync(() => CreateDirect(image));

                return operation.Task;
            }
            else
            {
                return Task.FromResult(CreateDirect(image));
            }

            Result<FrameworkElement> CreateDirect(BitmapImage image)
                => new Result<FrameworkElement>(new Image { Source = image });
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
