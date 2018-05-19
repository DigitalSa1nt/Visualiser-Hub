using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Visualiser_Hub.Utility_Classes
{
    class SourcetoImage
    {

        public BitmapImage Convert (BitmapSource btSource)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            MemoryStream memoryStream = new MemoryStream();
            BitmapImage bImg = new BitmapImage();

            encoder.QualityLevel = 100;
            encoder.Frames.Add(BitmapFrame.Create(btSource));
            encoder.Save(memoryStream);

            bImg.BeginInit();
            bImg.StreamSource = new MemoryStream(memoryStream.ToArray());
            bImg.Rotation = Rotation.Rotate180;
            bImg.EndInit();

            memoryStream.Close();

            return bImg;
        }

        public BitmapImage Convert_NoRotate(BitmapSource btSource)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            MemoryStream memoryStream = new MemoryStream();
            BitmapImage bImg = new BitmapImage();

            encoder.QualityLevel = 100;
            encoder.Frames.Add(BitmapFrame.Create(btSource));
            encoder.Save(memoryStream);

            bImg.BeginInit();
            bImg.StreamSource = new MemoryStream(memoryStream.ToArray());
            bImg.EndInit();

            memoryStream.Close();

            return bImg;
        }
    }
}
