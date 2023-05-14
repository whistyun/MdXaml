using MdXaml.Plugins;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace MdXaml.AnimatedGif
{
    public class AnimatedGifLoader : IElementLoader, IPreferredLoader
    {
        private static readonly byte[] G87AMagic = Encoding.ASCII.GetBytes("GIF87a");
        private static readonly byte[] G89AMagic = Encoding.ASCII.GetBytes("GIF89a");
        private static readonly byte[] NetscapeMagic = Encoding.ASCII.GetBytes("NETSCAPE2.0");
        private static readonly int MagicLength = G87AMagic.Length;

        public FrameworkElement? Load(Stream stream)
        {
            if (!CheckGifAFormat(stream))
                return null;

            stream.Position = 0;

            var memstr = new MemoryStream();
            stream.CopyTo(memstr);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = memstr;
            bitmap.EndInit();

            var image = new Image();
            ImageBehavior.SetRepeatBehavior(image, RepeatBehavior.Forever);
            ImageBehavior.SetAnimatedSource(image, bitmap);

            return image;
        }

        public bool CheckGifAFormat(Stream stream)
        {
            byte[] buffer = new byte[768];

            if (stream.Read(buffer, 0, MagicLength) != MagicLength)
                return false;

            if (!SeqEq(buffer, G87AMagic, MagicLength)
             && !SeqEq(buffer, G89AMagic, MagicLength))
                return false;

            if (!TryReadUShortS(stream, buffer, out var width))
                return false;

            if (!TryReadUShortS(stream, buffer, out var height))
                return false;

            if (!TryReadByteS(stream, buffer, out var packed))
                return false;

            if (!TryReadByteS(stream, buffer, out var bgIndex))
                return false;

            stream.Position++;

            var noTrailer = true;
            while (noTrailer)
            {

                if (!TryReadByteS(stream, buffer, out var blockType))
                    return false;

                switch ((int)blockType)
                {
                    case 0: // Empty
                        break;

                    case 0x21: // EXTENSION
                        if (!TryReadByteS(stream, buffer, out var extType))
                            return false;

                        switch ((int)extType)
                        {
                            case 0xF9: //GRAPHICS_CONTROL
                                if (!TryReadBlock(stream, buffer, out var _))
                                    return false;
                                break;

                            case 0xFF: //APPLICATION
                                if (!TryReadBlock(stream, buffer, out var blockLen))
                                    return false;

                                if (blockLen < NetscapeMagic.Length)
                                    return false;

                                if (SeqEq(buffer, NetscapeMagic, NetscapeMagic.Length))
                                {
                                    var count = 0;

                                    while (count > 0)
                                        if (!TryReadBlock(stream, buffer, out count))
                                            return false;
                                }
                                else if (!TryReadBlock(stream, buffer, out var _))
                                    return false;

                                break;
                        }
                        break;


                    case 0x2C:// IMAGE_DESCRIPTOR

                        // frame bounds ( X, Y, W, H)
                        foreach (var _ in Enumerable.Range(0, 4))
                            if (!TryReadUShortS(stream, buffer, out var _))
                                return false;

                        if (!TryReadByteS(stream, buffer, out var descPack))
                            return false;

                        if ((descPack & 0x80) != 0)
                        {
                            var colorSize = 2 << (descPack & 7);
                            if (stream.Read(buffer, 0, colorSize * 3) < colorSize * 3)
                                return false;
                        }

                        if (!TryReadByteS(stream, buffer, out var _))
                            return false;

                        if (!TryReadBlock(stream, buffer, out var _))
                            return false;

                        break;

                    case 0x3B: // TRAILER
                        noTrailer = false;
                        break;

                    default:
                        if (!TryReadBlock(stream, buffer, out var _))
                            return false;

                        break;
                }
            }

            var globalColorSz = 2 << (packed & 7);
            if (stream.Read(buffer, 0, globalColorSz * 3) < globalColorSz * 3)
                return false;

            return true;
        }

        private static bool SeqEq(byte[] a, byte[] b, int len)
        {
            if (a.Length < len || b.Length < len)
                return false;

            for (int i = 0; i < len; ++i)
                if (!a[i].Equals(b[i]))
                    return false;

            return true;
        }

        private static bool TryReadUShortS(Stream stream, byte[] buffer, out ushort read)
        {
            if (stream.Read(buffer, 0, 2) < 2)
            {
                read = default;
                return false;
            }

            read = (ushort)(buffer[0] | (buffer[1] << 8));
            return true;
        }

        private static bool TryReadByteS(Stream stream, byte[] buffer, out byte read)
        {
            if (stream.Read(buffer, 0, 1) < 1)
            {
                read = default;
                return false;
            }

            read = buffer[0];
            return true;
        }

        private static bool TryReadBlock(Stream stream, byte[] buffer, out int blockSize)
        {
            if (!TryReadByteS(stream, buffer, out var len))
            {
                blockSize = -1;
                return false;
            }

            blockSize = (int)len;
            var readLen = stream.Read(buffer, 0, blockSize);

            if (readLen < blockSize)
                return false;

            return true;
        }
    }
}
