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

namespace Dml_Editor.Panels
{
    public partial class MatrixValue : ValueControl
    {
        #region "To/From DmlPrimitiveInfo"

        bool Loading = false;

        private DmlPrimitive Primitive
        {
            get
            {
                return m_PrimitiveInfo.Primitive;
            }
        }

        private DmlPrimitiveInfo m_PrimitiveInfo;
        public DmlPrimitiveInfo PrimitiveInfo
        {
            get
            {
                return m_PrimitiveInfo;
            }
            set
            {
                ResetState();
                Loading = true;
                try
                {
                    m_PrimitiveInfo = value;

                    DGV.Rows.Clear();
                    DGV.Columns.Clear();

                    long nColumns = 0, nRows = 0;
                    if (Primitive is DmlArray) 
                    {
                        nColumns = 1;
                        nRows = ((DmlArray)Primitive).ArrayLength;
                        LoadArrayTypeList();
                    }
                    else if (Primitive is DmlMatrix)
                    {
                        nColumns = ((DmlMatrix)Primitive).Columns;
                        nRows = ((DmlMatrix)Primitive).Rows;
                        LoadMatrixTypeList();
                    }
                    else throw new Exception("Expected array or matrix type in matrix value display.");

                    DGV.AllowUserToAddRows = false;
                    for (int ii = 0; ii < nColumns; ii++) DGV.Columns.Add("C" + ii, "Column " + (ii + 1).ToString());
                    for (int ii = 0; ii < nRows; ii++) DGV.Rows.Add();
                    
                    for (int iRow = 0; iRow < nRows; iRow++)
                    {
                        for (int iCol = 0; iCol < nColumns; iCol++)
                        {
                                /** Arrays **/
                            if (Primitive is DmlSByteArray)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(sbyte);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlSByteArray)Primitive).GetElement(iRow).ToString();
                            }
                            else if (Primitive is DmlInt16Array)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(Int16);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlInt16Array)Primitive).GetElement(iRow).ToString();
                            }
                            else if (Primitive is DmlInt32Array)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(Int32);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlInt32Array)Primitive).GetElement(iRow).ToString();
                            }
                            else if (Primitive is DmlInt64Array)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(Int64);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlInt64Array)Primitive).GetElement(iRow).ToString();
                            }
                            else if (Primitive is DmlByteArray)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(byte);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlByteArray)Primitive).GetElement(iRow).ToString();
                            }
                            else if (Primitive is DmlUInt16Array)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(UInt16);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlUInt16Array)Primitive).GetElement(iRow).ToString();
                            }
                            else if (Primitive is DmlUInt32Array)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(UInt32);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlUInt32Array)Primitive).GetElement(iRow).ToString();
                            }
                            else if (Primitive is DmlUInt64Array)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(UInt64);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlUInt64Array)Primitive).GetElement(iRow).ToString();
                            }
                            else if (Primitive is DmlSingleArray)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(float);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlSingleArray)Primitive).GetElement(iRow).ToString();
                            }
                            else if (Primitive is DmlDoubleArray)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(double);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlDoubleArray)Primitive).GetElement(iRow).ToString();
                            }
                            else if (Primitive is DmlStringArray)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(string);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlStringArray)Primitive).GetElement(iRow).ToString();
                            }
                            else if (Primitive is DmlDateTimeArray)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(DateTime);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlDateTimeArray)Primitive).GetElement(iRow).ToString();
                            }
                                /** Matrices **/
                            else if (Primitive is DmlSByteMatrix)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(sbyte);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlSByteMatrix)Primitive).GetElement(iRow, iCol).ToString();
                            }
                            else if (Primitive is DmlInt16Matrix)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(Int16);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlInt16Matrix)Primitive).GetElement(iRow,iCol).ToString();
                            }
                            else if (Primitive is DmlInt32Matrix)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(Int32);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlInt32Matrix)Primitive).GetElement(iRow,iCol).ToString();
                            }
                            else if (Primitive is DmlInt64Matrix)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(Int64);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlInt64Matrix)Primitive).GetElement(iRow,iCol).ToString();
                            }
                            else if (Primitive is DmlByteMatrix)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(byte);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlByteMatrix)Primitive).GetElement(iRow,iCol).ToString();
                            }
                            else if (Primitive is DmlUInt16Matrix)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(UInt16);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlUInt16Matrix)Primitive).GetElement(iRow,iCol).ToString();
                            }
                            else if (Primitive is DmlUInt32Matrix)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(UInt32);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlUInt32Matrix)Primitive).GetElement(iRow,iCol).ToString();
                            }
                            else if (Primitive is DmlUInt64Matrix)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(UInt64);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlUInt64Matrix)Primitive).GetElement(iRow,iCol).ToString();
                            }
                            else if (Primitive is DmlSingleMatrix)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(float);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlSingleMatrix)Primitive).GetElement(iRow,iCol).ToString();
                            }
                            else if (Primitive is DmlDoubleMatrix)
                            {
                                DGV.Rows[iRow].Cells[iCol].ValueType = typeof(double);
                                DGV.Rows[iRow].Cells[iCol].Value = ((DmlDoubleMatrix)Primitive).GetElement(iRow,iCol).ToString();
                            }
                        }
                    }
                    DGV.AllowUserToAddRows = true;

                    if (Primitive is DmlArray)
                    {
                        foreach (DmlType PossibleType in DmlType.ArrayTypeList)
                        {
                            if (Primitive.GetType() == PossibleType.ObjType)
                            {
                                if (ElementTypeBox.SelectedItem != PossibleType)
                                    ElementTypeBox.SelectedItem = PossibleType;
                                break;
                            }
                        }
                    }
                    if (Primitive is DmlMatrix)
                    {
                        foreach (DmlType PossibleType in DmlType.MatrixTypeList)
                        {
                            if (Primitive.GetType() == PossibleType.ObjType)
                            {
                                if (ElementTypeBox.SelectedItem != PossibleType)
                                    ElementTypeBox.SelectedItem = PossibleType;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    Loading = false;
                }
            }
        }        

        void StoreDGVToDml()
        {
            if (Loading) return;

            try
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

                // Next, convert it all to a DmlPrimitive...
                if (ElementType is DmlArrayType)
                {
                    DmlArray Column = ((DmlArrayType)ElementType).CreateDmlArray(PrimitiveInfo.Node.Document, RowCount);
                    Column.Name = Primitive.Name;
                    for (int iRow = 0; iRow < RowCount; iRow++)
                    {
                        try
                        {
                            Column.SetElement(iRow, ((DmlArrayType)ElementType).ConvertToElement(DGV.Rows[iRow].Cells[0].Value as string));
                        }
                        catch (Exception)
                        {
                            //DGV.Rows[iRow].Cells[0].Style.BackColor = Color.Red;
                            throw new Exception("Cannot convert row #" + (iRow + 1).ToString() + " to type '" + ElementType.ToString() + "'.");
                        }
                    }                    
                    DoReplacePrimitive(Column);
                }
                else if (ElementType is DmlMatrixType)
                {
                    DmlMatrix Data = ((DmlMatrixType)ElementType).CreateDmlMatrix(PrimitiveInfo.Node.Document, RowCount, DGV.Columns.Count);
                    Data.Name = Primitive.Name;
                    for (int iRow = 0; iRow < RowCount; iRow++)
                    {
                        for (int iCol = 0; iCol < DGV.Columns.Count; iCol++)
                        {
                            try
                            {
                                Data.SetElement(iRow, iCol, ((DmlMatrixType)ElementType).ConvertToElement(DGV.Rows[iRow].Cells[iCol].Value as string));
                            }
                            catch (Exception)
                            {
                                throw new Exception("Cannot convert row #" + (iRow + 1).ToString() + " column #" + (iCol + 1).ToString() + " to type '" + ElementType.ToString() + "'.");
                            }
                        }
                    }                    
                    DoReplacePrimitive(Data);
                }
                else throw new Exception();

                DoSetError("");
            }
            catch (Exception ex)
            {
                DoSetError(ex.Message);
            }
        }

        #endregion

        public DmlType ElementType
        {
            get
            {
                return ElementTypeBox.SelectedItem as DmlType;
            }
        }

        public MatrixValue()
        {
            InitializeComponent();            
        }

        void LoadArrayTypeList()
        {            
            ElementTypeBox.Items.Clear();
            foreach (DmlArrayType typ in DmlType.ArrayTypeList) ElementTypeBox.Items.Add(typ);
        }

        void LoadMatrixTypeList()
        {
            ElementTypeBox.Items.Clear();
            foreach (DmlMatrixType typ in DmlType.MatrixTypeList) ElementTypeBox.Items.Add(typ);
        }

        private void MatrixValue_Load(object sender, EventArgs e)
        {

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
            ResetState();
        }

        void OnAddColumn(object sender, EventArgs ea)
        {
            ResetState();
            DGV.Columns.Add("C" + (DGV.Columns.Count + 1).ToString(), "Column " + (DGV.Columns.Count + 1).ToString());
        }

        void OnAddRow(object sender, EventArgs ea)
        {
            ResetState();
            DGV.Rows.Add();
        }

        void OnCopy(object sender, EventArgs ea)
        {
            ResetState();
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

        void ResetState()
        {
            DoClearError();
            foreach (DataGridViewCell cell in DGV.SelectedCells)
                cell.Style.SelectionBackColor = DGV.DefaultCellStyle.SelectionBackColor;
        }

        void OnPaste(object sender, EventArgs ea)
        {
            ResetState();
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
                MessageBox.Show("The data you pasted is in the wrong format for the cell.");
                return;
            }
            if (Examine(true)) DoSummaryChanged();
        }

        /// <summary>
        /// Examine() performs examination of the overall configuration and takes any necessary actions.
        /// </summary>
        /// <param name="StoreChanges">True if StoreDGVToDml() is needed during the examination.</param>
        /// <returns>True if DoSummaryChanged() should be called after completion.  False if DoSummaryChanged()
        /// is unnecessary for the purposes of Examine().</returns>
        bool Examine(bool StoreChanges)
        {
            try
            {
                bool SummaryChanged = false;
                
                DmlType CurrType = ElementTypeBox.SelectedItem as DmlType;
                if (CurrType != null && CurrType.ObjType != Primitive.GetType()) { StoreDGVToDml(); StoreChanges = false; }

                if (Primitive is DmlArray && DGV.Columns.Count > 1)
                {
                    DmlType NewType = DmlType.GetDmlMatrixType(((DmlArray)Primitive).ArrayType);
                    if (NewType == null) throw new Exception("Requested element type is not supported in matrix form.");
                    DoSelectedTypeChange(NewType);
                    SummaryChanged = true;
                    StoreChanges = true;
                }
                
                // Although we can automatically upgrade an array to a matrix, we don't want to go the other direction
                // without user interaction in case they intend to input a 1-column matrix.

                if (StoreChanges) StoreDGVToDml();
                return SummaryChanged;
            }
            catch (Exception ex)
            {
                DoSetError(ex.Message);
                return false;
            }
        }

        private void DGV_SelectionChanged(object sender, EventArgs e)
        {
            ResetState();
            if (Examine(false)) DoSummaryChanged();
        }

        private void DGV_CurrentCellChanged(object sender, EventArgs e)
        {
            ResetState();
            if (Examine(false)) DoSummaryChanged();
        }

        private void ElementTypeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Loading) return;

            ResetState();            
            Examine(true);
            DoSummaryChanged();
        }

        private void DGV_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
        {
            ResetState();
            Examine(true);
            DoSummaryChanged();
        }

        private void DGV_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            ResetState();
            Examine(true);
            DoSummaryChanged();
        }

        private void DGV_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            ResetState();
            if (Examine(true)) DoSummaryChanged();            
        }

        private void DGV_KeyDown(object sender, KeyEventArgs e)
        {
            ResetState();
            if (e.KeyCode == Keys.C && e.Modifiers == Keys.Control) OnCopy(null, null);
            if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control) OnPaste(null, null);
            base.OnKeyDown(e);
        }
    }
}
