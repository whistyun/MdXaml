using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace MdXaml.Plugins
{
#if MIG_FREE
    internal 
#else
    public
#endif
    interface IImageLoader
    {
        public BitmapImage? Load(Stream stream);
    }
}
