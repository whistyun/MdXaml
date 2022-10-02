using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace MdXaml.Plugins
{
    public interface IImageLoader
    {
        public BitmapImage? Load(Stream stream);
    }
}
