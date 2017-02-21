using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WileyBlack.UserInterface
{
    public partial class ScatterPlot : Control, ISupportInitialize
    {
        #region "Properties / Control"

        public class Axis
        {
            /// <summary>
            /// Minimum value to display on the axis, or NaN to allow the plot to choose automatically.
            /// </summary>
            public double Minimum = double.NaN;

            /// <summary>
            /// Maximum value to display on the axis, or NaN to allow the plot to choose automatically.
            /// </summary>
            public double Maximum = double.NaN;

            // Identical to Minimum and Maximum if values are provided.  The current calculated minimum
            // and maximum if Minimum and/or Maximum are NaN.
            internal double CurrentMinimum;
            internal double CurrentMaximum;

            public int DecimalPlaces = 2;

            public string Label;
            public string Units;

            public Axis()
            {
            }
        }

        public Axis XAxis = new Axis(), YAxis = new Axis();

        public class DataSeries
        {
            /// <summary>
            /// Contains the X values to be plotted.  Entries may be marked as double.NaN in order
            /// to exclude them.
            /// </summary>
            public double[] X;

            /// <summary>
            /// Contains the Y values to be plotted.  Elements which are NaN will be excluded from
            /// the plot.
            /// </summary>
            public double[] Y;

            public string Name;

            /// <summary>
            /// Optional brush to apply to the series.
            /// </summary>
            public Brush Color;
        }

        public List<DataSeries> Series;

        #endregion

        #region "Initialization / Disposal"

        public ScatterPlot()
        {
            this.DoubleBuffered = true;            
            this.SetStyle(ControlStyles.UserPaint |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.ResizeRedraw |
                          ControlStyles.ContainerControl |
                          ControlStyles.OptimizedDoubleBuffer,
                          //ControlStyles.SupportsTransparentBackColor, 
                          true);

            InitializeComponent();
        }        

        void ISupportInitialize.BeginInit()
        {
        }

        void ISupportInitialize.EndInit()
        {
        }

        #endregion

        #region "Operations"

        public void Reset()
        {
            XAxis = new Axis();
            YAxis = new Axis();
            Clear();
        }

        public void Clear()
        {            
            Series = new List<DataSeries>();
            Invalidate();
        }

        #endregion

        #region "Graph Analysis"

        /// <summary>
        /// In order to make some automated decisions about how the graph should appear,
        /// we need to estimate how cluttered the data is.  If it is only a few points
        /// compared to the plot area, then we can show large points connected by lines.
        /// If it is cluttered, we should just put small dots.
        /// 
        /// Precondition:  The CurrentMinimum and CurrentMaximum values of both axes must
        /// be setup before calling.  Also, PlotArea should be setup before calling.
        /// </summary>
        /// <returns>An estimate of plot density.  [0,0.3] is a sparse plot.  [0.3,2.0] is
        /// mildly full.  Beyond 2.0 is getting cluttered.</returns>
        public float GetGraphDensity()
        {
            int Points = 0;
            foreach (DataSeries Data in Series)
            {
                if (Data.X.Length != Data.Y.Length) 
                    throw new Exception("Data series '" + Data.Name + "' X and Y data lengths must match.");

                for (int ii = 0; ii < Data.X.Length; ii++)
                {
                    double X = Data.X[ii];
                    if (X < XAxis.CurrentMinimum || X > XAxis.CurrentMaximum) continue;
                    double Y = Data.Y[ii];
                    if (Y < YAxis.CurrentMinimum || Y > YAxis.CurrentMaximum) continue;
                    Points++;
                }
            }

            return (float)Points / PlotArea.Width;
        }

        #endregion

        #region "Display"

        private Rectangle PlotArea;

        Brush[] DefaultBrushes = new Brush[] {
            Brushes.Black,
            Brushes.Blue,
            Brushes.Red,
            Brushes.Green,
            Brushes.Violet            
        };

        private class AxisLabel
        {
            public string Text;
            public int Position;
            public SizeF TextSize;
        }

        Font LabelFont = new Font("Ariel", 10.0f);

        List<AxisLabel> GenerateAxis(Graphics g, int LabelSpacing, int Width, Axis axis, StringFormat LabelFormat)
        {
            List<AxisLabel> ret = new List<AxisLabel>();            
            
            for (int xx = 0; xx < Width; xx += LabelSpacing)
            {
                AxisLabel al = new AxisLabel();
                al.Position = xx;

                double Fraction = (double)xx / (double)(Width - 1);
                double X = Fraction * axis.CurrentMaximum + (1.0 - Fraction) * axis.CurrentMinimum;

                al.Text = X.ToString("F0" + axis.DecimalPlaces);
                if (!string.IsNullOrEmpty(axis.Units)) al.Text = al.Text + " " + axis.Units;
                
                al.TextSize = g.MeasureString(al.Text, LabelFont);
                ret.Add(al);
            }

            return ret;
        }

        SizeF GetLargest(List<AxisLabel> Labels)
        {
            SizeF sz = new SizeF(0, 0);
            foreach (AxisLabel label in Labels)
            {
                if (label.TextSize.Width > sz.Width) sz.Width = label.TextSize.Width;
                if (label.TextSize.Height > sz.Height) sz.Height = label.TextSize.Height;
            }
            return sz;
        }

        void PreparePlot()
        {
            PlotArea = new Rectangle(0, 0, Width, Height);
            PlotArea.Inflate(-25, -25);

            XAxis.CurrentMinimum = XAxis.Minimum;
            XAxis.CurrentMaximum = XAxis.Maximum;
            YAxis.CurrentMinimum = YAxis.Minimum;
            YAxis.CurrentMaximum = YAxis.Maximum;

            if (double.IsNaN(XAxis.CurrentMinimum)) XAxis.CurrentMinimum = double.MaxValue;
            if (double.IsNaN(XAxis.CurrentMaximum)) XAxis.CurrentMaximum = double.MinValue;
            if (double.IsNaN(YAxis.CurrentMinimum)) YAxis.CurrentMinimum = double.MaxValue;
            if (double.IsNaN(YAxis.CurrentMaximum)) YAxis.CurrentMaximum = double.MinValue;

            foreach (DataSeries series in Series)
            {
                if (series.X.Length != series.Y.Length)
                    throw new Exception("Data series '" + series.Name + "' cannot contain X and Y arrays of different lengths.");

                for (int xx=0; xx < series.X.Length; xx++)
                {
                    double X = series.X[xx];
                    double Y = series.Y[xx];
                    if (!double.IsNaN(X))
                    {
                        if (double.IsNaN(XAxis.Minimum) && X < XAxis.CurrentMinimum) XAxis.CurrentMinimum = X;
                        if (double.IsNaN(XAxis.Maximum) && X > XAxis.CurrentMaximum) XAxis.CurrentMaximum = X;
                    }
                    if (!double.IsNaN(Y))
                    {
                        if (double.IsNaN(YAxis.Minimum) && Y < YAxis.CurrentMinimum) YAxis.CurrentMinimum = Y;
                        if (double.IsNaN(YAxis.Maximum) && Y > YAxis.CurrentMaximum) YAxis.CurrentMaximum = Y;
                    }
                }
            }

            if (double.IsNaN(XAxis.CurrentMinimum) || double.IsNaN(XAxis.CurrentMaximum)
             || double.IsNaN(YAxis.CurrentMinimum) || double.IsNaN(YAxis.CurrentMaximum))
                throw new Exception("No data..");
        }

        private string m_ErrorText = null;        
        public string ErrorText
        {
            get
            {
                return m_ErrorText;
            }
            set
            {
                m_ErrorText = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            try
            {
                base.OnPaint(pe);

                Graphics g = pe.Graphics;                
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 250, 220)), ClientRectangle);
                g.DrawRectangle(Pens.Black, new Rectangle(0, 0, Width - 1, Height - 1));

                try
                {
                    if (!string.IsNullOrEmpty(ErrorText)) throw new Exception(ErrorText);

                    if (Series == null || Series.Count < 1) throw new Exception("No data..");
                    PreparePlot();                    

                    /** Plan Axes Ticks **/

                    StringFormat HFormat = new StringFormat();
                    StringFormat VFormat = new StringFormat(StringFormatFlags.DirectionVertical);

                    const int XPixelsPerLabel = 50;
                    const int YPixelsPerLabel = 50;

                    List<AxisLabel> XLabels = GenerateAxis(g, XPixelsPerLabel, PlotArea.Width, XAxis, VFormat);
                    SizeF LargestXLabel = GetLargest(XLabels);
                    List<AxisLabel> YLabels = GenerateAxis(g, YPixelsPerLabel, PlotArea.Height, YAxis, HFormat);
                    SizeF LargestYLabel = GetLargest(YLabels);                    

                    PlotArea = new Rectangle(
                        (int)(PlotArea.Left + LargestXLabel.Width),
                        PlotArea.Top,
                        (int)(PlotArea.Width - LargestXLabel.Width),
                        (int)(PlotArea.Height - LargestYLabel.Width)
                        );

                    /** Plan Axes' Labels **/

                    SizeF szXAxisLabel = new SizeF(0, 0);
                    if (!string.IsNullOrEmpty(XAxis.Label)) szXAxisLabel = g.MeasureString(XAxis.Label, LabelFont);
                    
                    SizeF szYAxisLabel = new SizeF(0, 0);
                    if (!string.IsNullOrEmpty(YAxis.Label)) szYAxisLabel = g.MeasureString(YAxis.Label, LabelFont);

                    PlotArea = new Rectangle(
                        (int)(PlotArea.Left + szXAxisLabel.Height),
                        PlotArea.Top,
                        (int)(PlotArea.Width - szXAxisLabel.Height),
                        (int)(PlotArea.Height - szYAxisLabel.Height)
                        );

                    /** Plan drawing style **/

                    float GraphDensity = GetGraphDensity();

                    /** Draw Axes **/

                    // Since we alter the size of the plot area, we must regenerate the tick marks to match.
                    XLabels = GenerateAxis(g, XPixelsPerLabel, PlotArea.Width, XAxis, VFormat);                    
                    YLabels = GenerateAxis(g, YPixelsPerLabel, PlotArea.Height, YAxis, HFormat);                    

                    foreach (AxisLabel label in XLabels)
                        g.DrawString(label.Text, LabelFont, Brushes.Black, 
                            PlotArea.Left + label.Position - label.TextSize.Height / 2, PlotArea.Bottom + 10, VFormat);

                    foreach (AxisLabel label in YLabels)
                        g.DrawString(label.Text, LabelFont, Brushes.Black, 
                            PlotArea.Left - 10 - label.TextSize.Width, PlotArea.Bottom - label.Position - label.TextSize.Height / 2, HFormat);

                    /** Label Axes **/
                    
                    if (XAxis.Label != null)
                    {
                        g.DrawString(XAxis.Label, LabelFont, Brushes.Black, 
                            PlotArea.Left + PlotArea.Width / 2.0f - szXAxisLabel.Width / 2.0f, Height - 10 - szXAxisLabel.Height, HFormat);
                    }                                        

                    if (YAxis.Label != null)
                    {
                        g.DrawString(YAxis.Label, LabelFont, Brushes.Black, 
                            10, PlotArea.Top + PlotArea.Height / 2.0f - szYAxisLabel.Width / 2.0f, VFormat);
                    }

                    /** Draw Graph **/

                    g.FillRectangle(Brushes.White, PlotArea);
                    g.DrawRectangle(Pens.Black, PlotArea);

                    /** Draw Data **/

                    double XRange = XAxis.CurrentMaximum - XAxis.CurrentMinimum;
                    double YRange = YAxis.CurrentMaximum - YAxis.CurrentMinimum;

                    for (int iSeries = 0; iSeries < Series.Count; iSeries++)
                    {
                        DataSeries Data = Series[iSeries];
                    
                        Brush CurrentBrush = Data.Color;
                        if (CurrentBrush == null)
                        {
                            if (iSeries >= DefaultBrushes.Length)
                                throw new Exception("Not enough plot colors defined for required plots.");
                            CurrentBrush = DefaultBrushes[iSeries];
                        }

                        for (int ixx = 0; ixx < Data.X.Length; ixx++)
                        {
                            double X = Data.X[ixx];
                            if (double.IsNaN(X)) continue;
                            int xx = (int)Math.Round(PlotArea.Width * (X - XAxis.CurrentMinimum) / XRange);
                            if (xx < 0 || xx > PlotArea.Width) continue;

                            double Y = Data.Y[ixx];
                            if (double.IsNaN(Y)) continue;
                            int yy = PlotArea.Height - (int)Math.Round(PlotArea.Height * (Y - YAxis.CurrentMinimum) / YRange);
                            if (yy < 0 || yy > PlotArea.Height) continue;

                            if (GraphDensity < 0.03f)
                                g.FillEllipse(CurrentBrush, new Rectangle(PlotArea.Left + xx - 6, PlotArea.Top + yy - 6, 12, 12));
                            if (GraphDensity < 0.3f)
                                g.FillEllipse(CurrentBrush, new Rectangle(PlotArea.Left + xx - 4, PlotArea.Top + yy - 4, 8, 8));
                            else if (GraphDensity < 0.75f)
                                g.FillEllipse(CurrentBrush, new Rectangle(PlotArea.Left + xx - 2, PlotArea.Top + yy - 2, 4, 4));
                            else 
                                g.FillEllipse(CurrentBrush, new Rectangle(PlotArea.Left + xx, PlotArea.Top + yy, 2, 2));
                        }
                    }
                }
                catch (Exception ex)
                {
                    SizeF sz = g.MeasureString(ex.Message, ErrorFont);
                    g.DrawString(ex.Message, ErrorFont, Brushes.Black, (Width / 2) - (sz.Width / 2), (Height / 2) - (sz.Height / 2));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        static Font ErrorFont = new Font(FontFamily.GenericSansSerif, 12.0f);

        private void OnResize(object sender, EventArgs e)
        {            
            Invalidate();
        }

        #endregion
    }
}
