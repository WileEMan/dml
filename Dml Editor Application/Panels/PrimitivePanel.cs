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
using Dml_Editor.Panels;

namespace Dml_Editor
{
    public partial class PrimitivePanel : UserControl
    {
        #region "Properties"

        private DmlPrimitiveInfo m_SelectedPrimitiveInfo;
        public DmlPrimitiveInfo SelectedPrimitiveInfo
        {
            get
            {
                return m_SelectedPrimitiveInfo;
            }
            set
            {
                try
                {
                    HideValueDisplay();
                    m_SelectedPrimitiveInfo = value;

                    TypeBox.SelectedItem = SelectedPrimitiveType;                    

                    gbPrimitive.Text = (value.Primitive.IsAttribute) ? "Attribute" : "Element";

                    labelName.Visible = true;
                    if (NameBox.Text != value.Primitive.Name) NameBox.Text = value.Primitive.Name;
                    NameBox.Visible = true;
                    labelType.Visible = true;
                    TypeBox.Visible = true;                                        
                    if (SelectedPrimitiveType != null) SelectedPrimitiveType.ShowValue(this, value.Primitive);
                }
                catch (Exception ex)
                {
                    OnSetError(ex.Message);
                }
            }
        }

        public DmlPrimitive SelectedPrimitive
        {
            get { return SelectedPrimitiveInfo.Primitive; }
        }

        public DmlType SelectedPrimitiveType
        {
            get
            {
                if (SelectedPrimitive == null) return null;                
                foreach (DmlType PossibleType in DmlType.Types)
                {
                    if (SelectedPrimitive.GetType() == PossibleType.ObjType) return PossibleType;
                }
                foreach (DmlArrayType PossibleType in DmlType.ArrayTypeList)
                {
                    if (SelectedPrimitive.GetType() == PossibleType.ObjType) return PossibleType;
                }
                foreach (DmlMatrixType PossibleType in DmlType.MatrixTypeList)
                {
                    if (SelectedPrimitive.GetType() == PossibleType.ObjType) return PossibleType;
                }
                return null;
            }
        }

        #endregion
        
        public MatrixValue MatrixValue = new MatrixValue();

        public PrimitivePanel()
        {
            InitializeComponent();            

            MatrixValue.Top = ValueTextBox.Top;
            MatrixValue.Left = ValueTextBox.Left;
            MatrixValue.OnSummaryChanged += new ValueControl.OnSummaryChangedHandler(OnValueSummaryChanged);
            MatrixValue.SetError += new ValueControl.SetErrorHandler(OnSetError);
            MatrixValue.ReplacePrimitive += new ValueControl.ReplacePrimitiveHandler(OnReplacePrimitive);
            MatrixValue.SelectedTypeChange += new ValueControl.SelectedTypeChangeHandler(OnSelectedTypeChanged);
            Controls.Add(MatrixValue);

            HideValueDisplay();
            
            foreach (DmlType typ in DmlType.Types) TypeBox.Items.Add(typ);            
        }

        void OnSelectedTypeChanged(DmlType NewType)
        {
            if (NewType is DmlArrayType) TypeBox.SelectedItem = DmlType.DmlArrayType;
            else if (NewType is DmlMatrixType) TypeBox.SelectedItem = DmlType.DmlMatrixType;
            else TypeBox.SelectedItem = NewType;
        }

        void OnReplacePrimitive(DmlPrimitive NewPrimitive)
        {
            ReplacePrimitive(NewPrimitive);
        }

        void OnSetError(string Message)
        {
            if (string.IsNullOrEmpty(Message))
            {
                labelValueError.Visible = false;
            }
            else
            {
                labelValueError.Text = Message;
                labelValueError.Visible = true;
            }
        }        

        private void PrimitivePanel_Load(object sender, EventArgs e)
        {
        }

        public void HideValueDisplay()
        {            
            ValueTextBox.Visible = false;            
            rbTrueValue.Visible = false;
            rbFalseValue.Visible = false;
            DateTimePicker.Visible = false;
            labelValueError.Visible = false;
            MatrixValue.Visible = false;            
        }

        // See also DmlType.ShowValue(..)...

        public delegate void OnSummaryChangedHandler();
        public event OnSummaryChangedHandler OnSummaryChanged;

        private void NameBox_Validated(object sender, EventArgs e)
        {
            SelectedPrimitive.Name = NameBox.Text;
            Control PrevFocus = FindFocusedControl(this);
            if (OnSummaryChanged != null) OnSummaryChanged();
            if (PrevFocus != null && PrevFocus.Visible) PrevFocus.Focus();
        }        

        public delegate bool EnsurePrimitiveSetHandler(string PrimitiveSet);
        public event EnsurePrimitiveSetHandler EnsurePrimitiveSet;

        bool OnEnsurePrimitiveSet(DmlType NewType)
        {
            if (NewType is DmlIntType || NewType is DmlUIntType || NewType is DmlStringType
             || NewType is DmlBoolType || ((NewType is DmlArrayType) && ((DmlArrayType)NewType).ArrayType == ArrayTypes.U8))
            {
                // Base set does not require additional primitive sets.
                return true;
            }
            if (NewType is DmlDoubleType || NewType is DmlSingleType || NewType is DmlDateTimeType)
            {
                if (EnsurePrimitiveSet == null) return false;
                return EnsurePrimitiveSet("common");
            }
            if (NewType is DmlArrayType || NewType is DmlMatrixType)
            {
                if (EnsurePrimitiveSet == null) return false;
                return EnsurePrimitiveSet("arrays");
            }
            return false;
        }

        private void TypeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SelectedPrimitiveInfo == null) return;

            DmlType NewType = TypeBox.SelectedItem as DmlType;            
            if (!OnEnsurePrimitiveSet(NewType))
            {
                HideValueDisplay();
                labelValueError.Text = "Additional Primitive Set required for selected type.";
                labelValueError.Visible = true;
                return;
            }
            DmlPrimitive NewPrim = NewType.ConvertType(SelectedPrimitive);
            if (NewPrim == null)
            {
                HideValueDisplay();
                OnSetError("Cannot convert value to selected type.");                
                return;
            }
            ReplacePrimitive(NewPrim);
            labelValueError.Text = "";
            labelValueError.Visible = false;

            if (OnSummaryChanged != null) OnSummaryChanged();
            if (SelectedPrimitiveType != null) SelectedPrimitiveType.ShowValue(this, SelectedPrimitiveInfo.Primitive);
        }

        public delegate void ReplacePrimitiveHandler(DmlPrimitive NewPrimitive);
        public event ReplacePrimitiveHandler ReplacePrimitive;        

        bool OnValueText()
        {
            labelValueError.Visible = false;

            if (SelectedPrimitiveType is DmlTextType)
            {
                try
                {
                    ((DmlTextType)SelectedPrimitiveType).UpdateValue(SelectedPrimitive, ValueTextBox.Text);
                    return true;
                }
                catch (FormatException)
                {
                    OnSetError("Invalid value for selected type.");
                    return false;
                }
            }
            else return true;
        }

        private void OnValueTextChanged(object sender, EventArgs e)
        {
            OnValueText();
        }

        private void OnValidateValueText(object sender, CancelEventArgs e)
        {
            if (!OnValueText())
            {
                e.Cancel = true;
            }            
        }

        bool InValidated = false;
        private void ValueTextBox_Validated(object sender, EventArgs e)
        {
            if (InValidated) return;        // Prevent mutual recursion.  Probably not actually necessary, issue resolved in OnSummaryChanged().
            InValidated = true;
            try
            {
                //Control PrevFocus = FindFocusedControl(this);
                if (OnSummaryChanged != null) OnSummaryChanged();                
                // if (PrevFocus != null && PrevFocus.Visible) PrevFocus.Focus();                
            }
            finally { InValidated = false; }
        }

        private void rbTrueValue_CheckedChanged(object sender, EventArgs e)
        {
            (SelectedPrimitive as DmlBool).Value = true;
            if (OnSummaryChanged != null) OnSummaryChanged();            
        }

        private void rbFalseValue_CheckedChanged(object sender, EventArgs e)
        {
            (SelectedPrimitive as DmlBool).Value = false;
            if (OnSummaryChanged != null) OnSummaryChanged();
        }

        private void DateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            DateTime Prev = (DateTime)(SelectedPrimitive as DmlDateTime).Value;
            if (DateTimePicker.Value != Prev)
            {
                (SelectedPrimitive as DmlDateTime).Value = DateTimePicker.Value;
                if (OnSummaryChanged != null) OnSummaryChanged();
            }
        }

        private void OnValueSummaryChanged()
        {
            if (OnSummaryChanged != null) OnSummaryChanged();
        }

        #region "Misc./Utility"
        public static Control FindFocusedControl(Control control)
        {
            var container = control as ContainerControl;
            while (container != null)
            {
                control = container.ActiveControl;
                container = control as ContainerControl;
            }
            return control;
        }
        #endregion                
    }
}
