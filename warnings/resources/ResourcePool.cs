using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using warnings.Properties;
using warnings.util;

namespace warnings.resources
{
    /* Handling all the resources and provide APIs to access to all those resources.*/
    public static class ResourcePool
    {
        /* Get the image source of the red R icon. */
        public static ImageSource GetIcon()
        {
            var bmp = new Bitmap(Resources.redr);
            var converter = new Bitmap2SourceConverter();
            return (ImageSource) converter.Convert(bmp, typeof (BitmapSource), null, null);
        }
    }
}
