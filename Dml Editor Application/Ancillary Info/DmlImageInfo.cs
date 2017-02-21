using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using WileyBlack.Dml;
using WileyBlack.Dml.Dom;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Dml_Editor
{
    public class DmlChannelInfo : DmlContainerInfo
    {
        public string Format
        {
            get
            {
                if (Container.Attributes["Format"] as DmlString == null) return "Y";
                return (string)((DmlString)Container.Attributes["Format"]).Value;
            }
            set
            {
                Container.SetAttribute("Format", value);
            }
        }

        public DmlMatrix Bitmap
        {
            get
            {
                if (Container.Children["Bitmap"] as DmlMatrix == null) return null;
                return (DmlMatrix)Container.Children["Bitmap"];
            }
            set
            {
                for (int ii = 0; ii < Container.Children.Count; )
                {
                    if (Container.Children[ii].Name == "Bitmap") Container.Children.RemoveAt(ii);
                    else ii++;
                }
                value.Name = "Bitmap";
                Container.Children.Add(value);
            }
        }

        public DmlChannelInfo(DmlContainer dml, DmlImageInfo ContainerInfo)
            : base(dml, ContainerInfo)
        {
        }

        public DmlChannelInfo(DmlContainer Parent, DmlImageInfo ContainerInfo, string Format, DmlMatrix Bitmap)
        {
            this.ContainerInfo = ContainerInfo;

            Container = new DmlContainer(Parent.Document);
            Container.Name = "Channel";
            this.Format = Format;
            this.Bitmap = Bitmap;
        }
    }

    public class DmlImageInfo : DmlContainerInfo
    {
        public bool AddCompression = false;

        public int Width
        {
            get
            {
                if (Container.Attributes["Width"] as DmlInt == null) return 0;
                DmlInt DmlWidth = ((DmlInt)Container.Attributes["Width"]);
                return (int)(long)DmlWidth.Value;
            }
            set
            {
                Container.SetAttribute("Width", value);
            }
        }

        public int Height
        {
            get
            {
                if (Container.Attributes["Height"] as DmlInt == null) return 0;
                return (int)(long)((DmlInt)Container.Attributes["Height"]).Value;
            }
            set
            {
                Container.SetAttribute("Height", value);
            }
        }

        private DmlChannelInfo[] m_Channels;
        public DmlChannelInfo[] Channels
        {
            get
            {
                return m_Channels;
            }
            set
            {
                m_Channels = value;

                RemoveDmlChannels(Container);

                if (!IsCompressed && AddCompression)
                {
                    DmlCompressed dc = new DmlCompressed(Container);
                    foreach (DmlChannelInfo dci in value) dc.DecompressedFragment.Children.Add(dci.Container);
                    Container.Children.Add(dc);
                }
                else
                {
                    foreach (DmlChannelInfo dci in value) Container.Children.Add(dci.Container);
                }
            }
        }

        private void RemoveDmlChannels(DmlFragment From)
        {
            for (int ii = 0; ii < From.Children.Count; )
            {
                if (From.Children[ii] is DmlCompressed)
                {
                    DmlCompressed CContent = (DmlCompressed)From.Children[ii];
                    RemoveDmlChannels(CContent.DecompressedFragment);
                    if (CContent.DecompressedFragment.Children.Count == 0)
                        From.Children.RemoveAt(ii);
                    else
                        ii++;
                }
                else if (From.Children[ii].Name == "Channel" && From.Children[ii] is DmlContainer)
                    From.Children.RemoveAt(ii);
                else
                    ii++;
            }
        }

        public DmlImageInfo(DmlContainer Dml, DmlFragmentInfo ContainerInfo)
            : base(Dml, ContainerInfo)
        {
            if (Dml.Name != "Image")
                throw new FormatException("Cannot parse image from Dml.");
            
            m_Channels = ReadChannels(Container).ToArray();
        }

        private List<DmlChannelInfo> ReadChannels(DmlFragment Dml)
        {
            List<DmlChannelInfo> set = new List<DmlChannelInfo>();            
            foreach (DmlNode node in Dml.Children)
            {
                if (node is DmlCompressed)
                {
                    set.AddRange(ReadChannels(((DmlCompressed)node).DecompressedFragment));
                    AddCompression = true;
                    continue;
                }
                if (node.Name == "Channel" && node is DmlContainer)
                {
                    DmlChannelInfo dci = new DmlChannelInfo((DmlContainer)node, this);
                    set.Add(dci);
                }
            }
            return set;
        }

        public override bool IsRecognizedDetail(DmlNode Detail)
        {
            if (Detail is DmlContainer)
            {
                if (Detail.Name == "Channel") return true;
                return false;
            }
            if (Detail is DmlPrimitive)
            {
                if (Detail.Name == "Width") return true;
                if (Detail.Name == "Height") return true;
                return false;
            }
            return false;
        }

        private DmlChannelInfo GetChannel(string Format)
        {
            foreach (DmlChannelInfo dci in Channels)
            {
                if (dci.Format == Format) return dci;
            }
            return null;
        }

        unsafe public Bitmap ToBitmap()
        {
            if (Width < 1 || Height < 1 || Channels.Length < 1) return null;
#           if false
            if (GetChannel("Y") != null)
            {
                if (GetChannel("Y").Bitmap is DmlInt16Matrix)
                {
                    DmlInt16Matrix src = (DmlInt16Matrix)GetChannel("Y").Bitmap;
                    Bitmap ret = new Bitmap(Width, Height, PixelFormat.Format16bppGrayScale);
                    for (int yy = 0; yy < Height; yy++)
                    {
                        for (int xx = 0; xx < Width; xx++)
                        {
                            int Y = src.Values[yy,xx];
                            ret.SetPixel(xx, yy, Color.From
                        }
                    }
                    return ret;
                }
            }
#           endif
            if (GetChannel("RGB") != null || GetChannel("ARGB") != null)
            {
                DmlChannelInfo Channel = GetChannel("RGB");
                if (Channel == null) Channel = GetChannel("ARGB");
                if (Channel.Bitmap is DmlUInt32Matrix)
                {                    
                    uint[,] src = (uint[,])Channel.Bitmap.Value;
                    Bitmap ret = new Bitmap(Width, Height, (Channel.Format == "RGB") ? PixelFormat.Format32bppRgb : PixelFormat.Format32bppArgb);
                    BitmapData bd = ret.LockBits(new Rectangle(0,0, Width, Height), ImageLockMode.WriteOnly, ret.PixelFormat);
                    for (int yy = 0; yy < Height; yy++)
                    {
                        byte* pbScanline = (byte*)bd.Scan0 + (bd.Stride * yy);
                        uint* pLine = (uint*)pbScanline;
                                            
                        for (int xx = 0; xx < Width; xx++, pLine++) *pLine = src[yy,xx];                        
                    }
                    ret.UnlockBits(bd);
                    return ret;
                }
            }
#           if false
            if (GetChannel("R") != null && GetChannel("G") != null && GetChannel("B") != null)
            {                
                if (GetChannel("RGBA").Bitmap is DmlUInt32Matrix)
                {
                    DmlUInt32Matrix src = (DmlUInt32Matrix)GetChannel("RGBA").Bitmap;
                    Bitmap ret = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
                    for (int yy = 0; yy < Height; yy++)
                    {
                        for (int xx = 0; xx < Width; xx++)
                        {
                            int argb = (int)src.Values[yy, xx];
                            ret.SetPixel(xx, yy, Color.FromArgb(argb));
                        }
                    }
                    return ret;
                }
            }
#           endif
            throw new NotSupportedException("Unsupported image format.");
        }

        unsafe public void FromBitmap(Bitmap src)
        {
            Width = src.Width;
            Height = src.Height;
            Rectangle rect = new Rectangle(0, 0, src.Width, src.Height);            

            switch (src.PixelFormat)
            {
                case PixelFormat.Format16bppGrayScale:
                    {
                        ushort[,] Y = new ushort[src.Height, src.Width];
                        BitmapData bd = src.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format16bppGrayScale);

                        for (int yy = 0; yy < Height; yy++)
                        {
                            byte* pbScanline = (byte*)bd.Scan0 + (bd.Stride * yy);
                            ushort* pLine = (ushort*)pbScanline;

                            for (int xx = 0; xx < Width; xx++, pLine++) Y[yy, xx] = *pLine;
                        }

                        src.UnlockBits(bd);
                        DmlUInt16Matrix YMatrix = new DmlUInt16Matrix(Container.Document, Y);
                        DmlChannelInfo YChannel = new DmlChannelInfo(Container, this, "Y", YMatrix);
                        Channels = new DmlChannelInfo[] { YChannel };
                        break;
                    }

                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppRgb:
                    {
                        uint[,] ARGB = new uint[src.Height, src.Width];
                        BitmapData bd = src.LockBits(rect, ImageLockMode.ReadOnly, src.PixelFormat);

                        for (int yy = 0; yy < Height; yy++)
                        {
                            byte* pbScanline = (byte*)bd.Scan0 + (bd.Stride * yy);
                            uint* pLine = (uint*)pbScanline;

                            for (int xx = 0; xx < Width; xx++, pLine++) ARGB[yy, xx] = *pLine;                                
                        }

                        src.UnlockBits(bd);
                        DmlUInt32Matrix ARGBMatrix = new DmlUInt32Matrix(Container.Document, ARGB);
                        DmlChannelInfo ARGBChannel = new DmlChannelInfo(Container, this,
                            src.PixelFormat == PixelFormat.Format32bppArgb ? "ARGB" : "RGB", ARGBMatrix);
                        Channels = new DmlChannelInfo[] { ARGBChannel };
                        break;
                    }

                default: throw new NotSupportedException("This image format is not supported.");
            }
        }
    }
}
