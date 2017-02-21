using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Dml_Editor
{
    public partial class ImagePanel : UserControl
    {
        static Font BasicFont = new Font(FontFamily.GenericSansSerif, 15.0f, FontStyle.Regular);

        bool bNoContent;

        private DmlImageInfo m_ImageInfo;
        public DmlImageInfo ImageInfo
        {
            get
            {
                return m_ImageInfo;
            }
            set
            {
                m_ImageInfo = value;

                try
                {
                    PictureBox.Image = value.ToBitmap();
                    if (PictureBox.Image == null)
                    {
                        bNoContent = true;
                        PictureBox.Image = CreateTextImage("No content.", Brushes.Black);
                    }
                    else bNoContent = false;
                    PictureBox.Visible = true;
                }
                catch (Exception ex)
                {
                    PictureBox.Image = CreateTextImage(ex.ToString(), Brushes.Red);
                    bNoContent = true;
                }
            }
        }

        private Bitmap CreateTextImage(string Message, Brush ForeColor)
        {
            Bitmap bmp = new Bitmap(PictureBox.Width, PictureBox.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Font TryFont = BasicFont;
                float FSize = 15.0f;
                SizeF sz;
                string Text = Message;
                for (; ; )
                {
                    g.FillRectangle(Brushes.White, new Rectangle(0, 0, PictureBox.Width, PictureBox.Height));                    
                    sz = g.MeasureString(Text, TryFont);
                    if (FSize >= 6.0f && sz.Width > bmp.Width)
                    {
                        FSize -= 2.0f;
                        TryFont = new Font(FontFamily.GenericSansSerif, FSize, FontStyle.Regular);
                    }
                    else break;
                }
                g.DrawString(Text, TryFont, ForeColor, new PointF(PictureBox.Width / 2.0f - sz.Width / 2.0f, PictureBox.Height / 2.0f - sz.Height - 2.0f));
            }
            return bmp;
        }

        public ImagePanel()
        {
            InitializeComponent();
        }        

        private void PictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            ContextMenu Menu = new ContextMenu();
            MenuItem mi = new MenuItem("&Copy Image", new EventHandler(OnCopy));
            mi.Enabled = !bNoContent;            
            Menu.MenuItems.Add(mi);
            Menu.MenuItems.Add(new MenuItem("&Paste Image", new EventHandler(OnPaste)));
            Menu.Show(PictureBox, new Point(e.X, e.Y));
        }

        void OnCopy(object sender, EventArgs ea)
        {
            if (!PictureBox.Visible) return;
            Clipboard.SetImage(PictureBox.Image);
        }

        void OnPaste(object sender, EventArgs ea)
        {
            Image FromClipboard = Clipboard.GetImage();
            if (FromClipboard as Bitmap == null) return;
            ImageInfo.FromBitmap((Bitmap)FromClipboard);
            ImageInfo = ImageInfo;          // Refresh the PictureBox from the DML-backed image

#           if false
            if (Clipboard.GetDataObject() == null) return;
            if (Clipboard.GetDataObject().GetDataPresent(DataFormats.Dib)) {
                Clipboard.GetImage();
                var dib = ((System.IO.MemoryStream)Clipboard.GetData(DataFormats.Dib)).ToArray();
                var width = BitConverter.ToInt32(dib, 4);
                var height = BitConverter.ToInt32(dib, 8);
                var bpp = BitConverter.ToInt16(dib, 14);
                if (bpp == 32) {
                    var gch = GCHandle.Alloc(dib, GCHandleType.Pinned);
                    Bitmap bmp = null;
                    try {
                        var ptr = new IntPtr((long)gch.AddrOfPinnedObject() + 40);
                        bmp = new Bitmap(width, height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, ptr);
                        return new Bitmap(bmp);
                    }
                    finally {
                        gch.Free();
                        if (bmp != null) bmp.Dispose();
                    }
                }
            }
            return Clipboard.ContainsImage() ? Clipboard.GetImage() : null;    
#           endif
        }        
    }
}
