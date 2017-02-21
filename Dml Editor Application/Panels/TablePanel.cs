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
using WileyBlack.UserInterface;

namespace Dml_Editor
{
    public partial class TablePanel : UserControl
    {
        #region "To/From DmlTableInfo"

        List<DmlArrayType> ColumnTypes = new List<DmlArrayType>();

        bool Loading = false;

        private DmlTableInfo m_TableInfo;
        public DmlTableInfo TableInfo
        {
            get
            {
                return m_TableInfo;
            }
            set
            {
                ResetCellStyle();
                Loading = true;
                try
                {
                    m_TableInfo = value;
                    TableErrorMsg.Visible = false;

                    DGV.Rows.Clear();
                    DGV.Columns.Clear();

                    for (int ii = 0; ii < TableInfo.Columns.Length; ii++)
                    {
                        if (string.IsNullOrEmpty(TableInfo.Columns[ii].Name))
                            DGV.Columns.Add("C" + ii, "C" + ii);
                        else
                            DGV.Columns.Add("C" + ii, TableInfo.Columns[ii].Name);
                    }
                    
                    long Rows = 0;
                    foreach (DmlColumnInfo Column in TableInfo.Columns)
                        Rows = Math.Max(Rows, Column.Data.ArrayLength);
                    if (Rows == 0) return;
                    DGV.AllowUserToAddRows = false;
                    for (int iCount = 0; iCount < Rows; iCount++) DGV.Rows.Add();
                    for (int iCol = 0; iCol < TableInfo.Columns.Length; iCol++)
                    {
                        DmlArray Column = TableInfo.Columns[iCol].Data;
                        ColumnTypes.Add(DmlType.GetDmlType(Column));
                        for (int iRow = 0; iRow < Column.ArrayLength; iRow++)
                        {
                            if (Column is DmlSByteArray)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(sbyte);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlSByteArray)Column).GetElement(iRow).ToString();
                            }
                            else if (Column is DmlInt16Array)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(Int16);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlInt16Array)Column).GetElement(iRow).ToString();
                            }
                            else if (Column is DmlInt32Array)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(Int32);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlInt32Array)Column).GetElement(iRow).ToString();
                            }
                            else if (Column is DmlInt64Array)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(Int64);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlInt64Array)Column).GetElement(iRow).ToString();
                            }
                            else if (Column is DmlByteArray)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(byte);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlByteArray)Column).GetElement(iRow).ToString();
                            }
                            else if (Column is DmlUInt16Array)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(UInt16);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlUInt16Array)Column).GetElement(iRow).ToString();
                            }
                            else if (Column is DmlUInt32Array)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(UInt32);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlUInt32Array)Column).GetElement(iRow).ToString();
                            }
                            else if (Column is DmlUInt64Array)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(UInt64);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlUInt64Array)Column).GetElement(iRow).ToString();
                            }
                            else if (Column is DmlSingleArray)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(float);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlSingleArray)Column).GetElement(iRow).ToString();
                            }
                            else if (Column is DmlDoubleArray)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(double);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlDoubleArray)Column).GetElement(iRow).ToString();
                            }
                            else if (Column is DmlStringArray)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(string);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlStringArray)Column).GetElement(iRow).ToString();
                            }
                            else if (Column is DmlDateTimeArray)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(DateTime);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlDateTimeArray)Column).GetElement(iRow).ToString();
                            }
                        }
                    }
                    // Always add an empty row as the "Allow user to add rows" row...
                    //DGV.Rows.Add();                    
                    DGV.AllowUserToAddRows = true;

                    StoreDGVToPlot();
                }
                finally
                {
                    Loading = false;
                }
            }
        }

        double[] ColumnToDoubles(int iCol)
        {
            int Rows = DGVRowsInUse;
            double[] ret = new double [Rows];
            for (int ii=0; ii < Rows; ii++)
            {
                try
                {
                    if (DGV.Rows[ii].Cells[iCol].Value == null) ret[ii] = double.NaN;
                    else ret[ii] = Convert.ToDouble(DGV.Rows[ii].Cells[iCol].Value);
                }
                catch (InvalidCastException) { ret[ii] = double.NaN; } 
            }
            return ret;
        }

        void StoreDGVToPlot()
        {
            Plot.Clear();
            
            try
            {
                if (DGV.Columns.Count < 2 || DGVRowsInUse < 2) throw new Exception();

                List<ScatterPlot.DataSeries> AllSeries = new List<ScatterPlot.DataSeries>();

                double[] X = ColumnToDoubles(0);
                for (int iCol = 1; iCol < DGV.Columns.Count; iCol++)
                {
                    double[] Y = ColumnToDoubles(iCol);
                    ScatterPlot.DataSeries Series = new ScatterPlot.DataSeries();
                    Series.X = X;
                    Series.Y = Y;
                    Series.Name = DGV.Columns[iCol].HeaderText;
                    AllSeries.Add(Series);
                }

                Plot.XAxis.Label = DGV.Columns[0].HeaderText;
                if (DGV.Columns.Count == 2) Plot.YAxis.Label = DGV.Columns[1].HeaderText;

                Plot.Series = AllSeries;
            }
            catch (Exception)
            {
                Plot.Clear();
                if (tabs.SelectedTab == tabGraph)
                tabs.SelectedTab = tabTable;
            }            
        }

        int DGVRowsInUse
        {
            get
            {
                // First, determine if the special last row is empty.  If it is, then we won't include it.
                int RowCount;
                if (DGV.Rows.Count == 0) RowCount = 0;
                else
                {
                    RowCount = DGV.Rows.Count - 1;
                    for (int iCol = 0; iCol < DGV.Columns.Count; iCol++)
                    {
                        object Value = DGV.Rows[RowCount].Cells[iCol].Value;
                        if (!(Value == null || (Value is string && string.IsNullOrEmpty(((string)Value)))))
                        {
                            // There's content in the last row.  Include it.
                            RowCount++;
                            break;
                        }
                    }
                }
                return RowCount;
            }
        }

        void StoreDGVToDml()
        {
            if (Loading) return;

            try
            {
                // First, determine if the special last row is empty.  If it is, then we won't include it.
                int RowCount = DGVRowsInUse;

                // Next, convert it all to a DmlTableInfo...
                List<DmlColumnInfo> Columns = new List<DmlColumnInfo>();                
                for (int iCol = 0; iCol < DGV.Columns.Count; iCol++)
                {
                    DmlContainer ColumnDml = new DmlContainer(TableInfo.Container.Document);
                    ColumnDml.Name = "Column";
                    DmlColumnInfo Column = new DmlColumnInfo(ColumnDml, TableInfo);
                    Column.Name = DGV.Columns[iCol].HeaderText;

                    DmlArray Data = ColumnTypes[iCol].CreateDmlArray(TableInfo.Node.Document, RowCount);
                    Data.Name = "Data";
                    for (int iRow = 0; iRow < RowCount; iRow++)
                    {
                        try
                        {
                            Data.SetElement(iRow, ColumnTypes[iCol].ConvertToElement(DGV.Rows[iRow].Cells[iCol].Value));
                        }
                        catch (Exception)
                        {
                            throw new Exception("Cannot convert row #" + (iRow + 1).ToString() + " column #" + (iCol + 1).ToString() + " to type '" + ColumnTypes[iCol].ToString() + "'.");
                        }
                    }
                    Column.Data = Data;
                    Columns.Add(Column);
                }
                TableInfo.Columns = Columns.ToArray();
                TableErrorMsg.Visible = false;
            }
            catch (Exception ex)
            {
                TableErrorMsg.Text = ex.Message;
                TableErrorMsg.Visible = true;
            }
        }

        #endregion

        //TextBox tbColumnName = new TextBox();

        ScatterPlot Plot;

        public TablePanel()
        {
            InitializeComponent();

            Plot = new ScatterPlot();
            Plot.Left = 10;
            Plot.Top = 10;
            Plot.Width = tabGraph.Width - 20;
            Plot.Height = tabGraph.Height - 20;
            tabGraph.Controls.Add(Plot);

// Default to the table until the graph is more functional.
//tabs.TabPages.Remove(tabGraph);
//tabs.SelectedTab = tabTable;

            foreach (DmlArrayType typ in DmlType.ArrayTypeList) TypeBox.Items.Add(typ);

            //Controls.Add(tbColumnName);
            //tbColumnName.Visible = false;            
        }

        private void TablePanel_Load(object sender, EventArgs e)
        {
            DGV_CurrentCellChanged(null, null);
        }

        private void DGV_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            ContextMenu Menu = new ContextMenu();
            MenuItem mi = new MenuItem("Add &Row", new EventHandler(OnAddRow));
            mi.Enabled = (DGV.Columns.Count > 0);
            Menu.MenuItems.Add(mi);
            Menu.MenuItems.Add(new MenuItem("Add &Column", new EventHandler(OnAddColumn)));
            Menu.MenuItems.Add("-");
            if (DGV.SelectedCells.Count > 0)
                Menu.MenuItems.Add(new MenuItem("Cop&y", new EventHandler(OnCopy)));
            else
                Menu.MenuItems.Add(new MenuItem("Cop&y Table", new EventHandler(OnCopy)));
            Menu.MenuItems.Add(new MenuItem("&Paste", new EventHandler(OnPaste)));
            Menu.Show(DGV, new Point(e.X, e.Y));
        }

        private void DGV_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            ResetCellStyle();
        }

        void OnAddColumn(object sender, EventArgs ea)
        {
            ResetCellStyle();
            ColumnTypes.Add(DmlType.DmlSArrayType);
            DGV.Columns.Add("C" + (DGV.Columns.Count + 1).ToString(), "New Column");            
        }

        void OnAddRow(object sender, EventArgs ea)
        {
            ResetCellStyle();
            DGV.Rows.Add();
        }

        void OnCopy(object sender, EventArgs ea)
        {
            ResetCellStyle();
            if (DGV.SelectedCells.Count == 0)
            {
                DGV.SelectAll();
                Clipboard.SetDataObject(DGV.GetClipboardContent(), true);
                for (int iRow = 0; iRow < DGV.Rows.Count; iRow++)
                    foreach (DataGridViewCell cell in DGV.Rows[iRow].Cells)
                        cell.Style.SelectionBackColor = Color.Yellow;
                DGV.ClearSelection();
            }
            else
            {
                Clipboard.SetDataObject(DGV.GetClipboardContent(), true);
                foreach (DataGridViewCell cell in DGV.SelectedCells)
                    cell.Style.SelectionBackColor = Color.Yellow;
            }
        }

        void ResetCellStyle()
        {
            foreach (DataGridViewCell cell in DGV.SelectedCells)
                cell.Style.SelectionBackColor = DGV.DefaultCellStyle.SelectionBackColor;
        }

        void OnPaste(object sender, EventArgs ea)
        {
            ResetCellStyle();
            try
            {
                string s = Clipboard.GetText();
                s = s.Replace('\r', ' ');
                string[] lines = s.Split('\n');
                int iFail = 0, iRow = DGV.CurrentCell.RowIndex;
                int iCol = DGV.CurrentCell.ColumnIndex;
                DataGridViewCell oCell;
                foreach (string line in lines)
                {
                    if (line.Length == 0) continue;
                    if (iRow > DGV.RowCount) DGV.Rows.Add();                    

                    string[] sCells = line.Split('\t');
                    for (int i = 0; i < sCells.GetLength(0); ++i)
                    {
                        string PasteValue = sCells[i].Trim();
                        if (iCol + i < DGV.ColumnCount)
                        {
                            oCell = DGV[iCol + i, iRow];
                            if (oCell.Value.ToString() != PasteValue)
                            {
                                if (oCell.ReadOnly) iFail++;
                                else oCell.Value = PasteValue;
                                //else oCell.Value = Convert.ChangeType(PasteValue, oCell.ValueType);
                            }
                        }
                        else break;
                    }
                    iRow++;                                        
                }

                if (iFail > 0) MessageBox.Show(string.Format("{0} cells could not be pasted because they are read only.", iFail));
            }
            catch (Exception)
            {
                MessageBox.Show("The data you pasted is in the wrong format for the cell");
                return;
            }            
            StoreDGVToDml();
        }
        
        private void DGV_ColumnHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            ResetCellStyle();

            DataGridView dgSender = (DataGridView)sender;
            /**
            tbColumnName.Left = dgSender.CurrentCell.ContentBounds.Left + dgSender.Left;
            tbColumnName.Top = dgSender.CurrentCell.ContentBounds.Top + dgSender.Top;
            tbColumnName.Width = ((DataGridView)sender).CurrentCell.ContentBounds.Width;
            tbColumnName.Height = ((DataGridView)sender).CurrentCell.ContentBounds.Height;
            tbColumnName.Dock = DockStyle.Fill;
            tbColumnName.Visible = true;
            tbColumnName.BringToFront();
            tbColumnName.Text = dgSender.Columns[dgSender.CurrentCell.ColumnIndex].HeaderText;
             */
            User_Interface.TextPromptForm tpf = new User_Interface.TextPromptForm();
            tpf.PromptLabel.Text = "";
            tpf.UserText.Text = dgSender.Columns[e.ColumnIndex].HeaderText;
            if (tpf.ShowDialog() != DialogResult.OK) return;
            dgSender.Columns[e.ColumnIndex].HeaderText = tpf.UserText.Text;
        }                

        private void DGV_SelectionChanged(object sender, EventArgs e)
        {
            ResetCellStyle();

            //TypeBox.SelectedItem = ColumnTypes[DGV.CurrentCell.ColumnIndex];
        }

        private void DGV_CurrentCellChanged(object sender, EventArgs e)
        {
            ResetCellStyle();

            if (DGV.CurrentCell == null) TypeBox.Enabled = false;
            else
            {
                TypeBox.Enabled = true;
                TypeBox.SelectedItem = ColumnTypes[DGV.CurrentCell.ColumnIndex];
            }
        }

        private void TypeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResetCellStyle();

            if (DGV.CurrentCell != null) 
            {                
                ColumnTypes[DGV.CurrentCell.ColumnIndex] = TypeBox.SelectedItem as DmlArrayType;
                StoreDGVToDml();
            }            
        }

        private void DGV_ColumnHeaderCellChanged(object sender, DataGridViewColumnEventArgs e)
        {
            ResetCellStyle();
            StoreDGVToDml();
        }

        private void DGV_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
        {
            ResetCellStyle();
            StoreDGVToDml();
        }

        private void DGV_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            ResetCellStyle();
            StoreDGVToDml();            
        }

        private void DGV_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            ResetCellStyle();
            StoreDGVToDml();
        }

        private void DGV_KeyDown(object sender, KeyEventArgs e)
        {
            ResetCellStyle();
            if (e.KeyCode == Keys.C && e.Modifiers == Keys.Control) OnCopy(null, null);
            if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control) OnPaste(null, null);
            base.OnKeyDown(e);
        }

        private void tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabs.SelectedTab == tabGraph)            
                StoreDGVToPlot();            
        }        
    }
}
