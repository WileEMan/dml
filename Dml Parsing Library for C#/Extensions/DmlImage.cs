using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using WileyBlack.Platforms;

namespace WileyBlack.Dml.Extensions
{
#if false
    public class DmlImage : IDmlReaderExtension
    {
        public enum NodeTypes : uint
        {
            Unidentified = 0,
            Image = 1,
            LosslessImage = 2
        }

        #region "Extension Support"

        bool JPEGFileLoaded = false;
        bool PNGFileLoaded = false;

        bool IDmlReaderExtension.AddPrimitiveSet(string SetName, string Codec)
        {
            if (SetName != "image-file") return false;
            if (Codec == "jpeg-file") { JPEGFileLoaded = true; return true; }
            if (Codec == "png-file") { PNGFileLoaded = true; return true; }
            return false;
        }        

        uint IDmlReaderExtension.Identify(string PrimitiveType)
        {
            if (JPEGFileLoaded && PrimitiveType == "image") return (uint)NodeTypes.Image;
            if (PNGFileLoaded && PrimitiveType == "lossless-image") return (uint)NodeTypes.LosslessImage;
            return 0;
        }

        #endregion

        #region "Reader Extension"

        Image CurrentNode = null;

        void IDmlReaderExtension.OpenNode(Association NodeInfo, EndianBinaryReader Reader)
        {
            switch ((NodeTypes)NodeInfo.DMLName.TypeId)
            {
                case NodeTypes.Image:
                case NodeTypes.LosslessImage:
                    CurrentNode = Bitmap.FromStream(Reader.BaseStream);
                    return;
                default: throw new NotSupportedException();
            }
        }

        public int Width { 
            get { 
                if (CurrentNode == null) throw new FormatException("No DmlImage node is currently open.  Call Read() before retrieving image properties.");
                return CurrentNode.Width;
            }
        }

        public int Height { 
            get { 
                if (CurrentNode == null) throw new FormatException("No DmlImage node is currently open.  Call Read() before retrieving image properties.");
                return CurrentNode.Height;
            }
        }

        public PixelFormat PixelFormat { 
            get { 
                if (CurrentNode == null) throw new FormatException("No DmlImage node is currently open.  Call Read() before retrieving image properties.");
                return CurrentNode.PixelFormat;
            }
        }

        object IDmlReaderExtension.GetNode(Association NodeInfo, EndianBinaryReader Reader)
        {
            switch ((NodeTypes)NodeInfo.DMLName.TypeId)
            {
                case NodeTypes.Image:
                case NodeTypes.LosslessImage:
                    Image ret = CurrentNode;
                    CurrentNode = null;
                    return ret;
                default: throw new NotSupportedException();
            }
        }

        void IDmlReaderExtension.CloseNode(Association NodeInfo, EndianBinaryReader Reader)
        {
            switch ((NodeTypes)NodeInfo.DMLName.TypeId)
            {
                case NodeTypes.Image:
                case NodeTypes.LosslessImage:
                    CurrentNode = null;
                    return;
                default: throw new NotSupportedException();
            }
        }

        #endregion

        #region "Writer Support"

        private ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
                if (codec.FormatID == format.Guid) return codec;
            return null;
        }

        /// <summary>
        /// Writes a bitmap image into a DML Node using lossy compression.  Call the DmlWriter.WriteStartExtension()
        /// just prior to using the Write() call.
        /// </summary>
        /// <param name="Writer">The writer returned from the DmlWriter.WriteStartExtension() call.</param>
        /// <param name="img">The image to be written.</param>
        /// <param name="Quality">Quality between 0 (lowest) to 100 (highest quality).</param>
        public void Write(EndianBinaryWriter Writer, Image img, int Quality)
        {
            ImageCodecInfo JPEG = GetEncoderInfo(ImageFormat.Jpeg);
            EncoderParameters Params = new EncoderParameters(1);
            Params.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)Quality);
            img.Save(Writer.BaseStream, JPEG, Params);
        }

        /// <summary>
        /// Writes a bitmap image into a DML Node using lossless compression.  Call the DmlWriter.WriteStartExtension()
        /// method just prior to using the Write() call.
        /// </summary>
        /// <param name="Writer">The writer returned from the DmlWriter.WriteStartExtension() call.</param>
        /// <param name="img">The image to be written.</param>
        public void Write(EndianBinaryWriter Writer, Image img)
        {
            img.Save(Writer.BaseStream, ImageFormat.Png);
        }

        #endregion
    }
#endif
}
