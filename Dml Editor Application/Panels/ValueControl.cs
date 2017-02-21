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
    public partial class ValueControl : UserControl
    {
        /// <summary>
        /// The SetError event is called when the red error label needs to be updated.  If set to
        /// a null or empty string, the error label should be made invisible.  DoSetError() only
        /// updates the error message if no prior error message has been set since the last
        /// DoClearError() call.
        /// </summary>
        public event SetErrorHandler SetError;
        public delegate void SetErrorHandler(string Message);
        protected void DoSetError(string Message)
        {
            if (!string.IsNullOrEmpty(ErrMsg)) return;
            ErrMsg = Message;
            if (SetError != null) SetError(Message);
        }
        string ErrMsg;
        protected void DoClearError() { ErrMsg = ""; }

        /// <summary>
        /// The OnSummaryChanged event is fired whenever the primitive's short summary (as would be
        /// displayed on the attributes or elements pane) has been updated.
        /// </summary>
        public event OnSummaryChangedHandler OnSummaryChanged;
        public delegate void OnSummaryChangedHandler();
        protected void DoSummaryChanged()
        {
            if (OnSummaryChanged != null) OnSummaryChanged();
        }

        /// <summary>
        /// The ReplacePrimitive event is fired when a new primitive object needs to replace the old.
        /// This occurs when the type changes - as opposed to just the value.
        /// </summary>
        public event ReplacePrimitiveHandler ReplacePrimitive;
        public delegate void ReplacePrimitiveHandler(DmlPrimitive NewPrimitive);
        protected void DoReplacePrimitive(DmlPrimitive NewPrimitive)
        {
            if (ReplacePrimitive != null) ReplacePrimitive(NewPrimitive);
        }

        /// <summary>
        /// The SelectedTypeChange event is fired when the user has explicitly requested a different
        /// type.  This can be indirect - for example the adding of a 2nd column to an array changes
        /// the type from an array to a matrix.
        /// </summary>
        public event SelectedTypeChangeHandler SelectedTypeChange;
        public delegate void SelectedTypeChangeHandler(DmlType NewType);
        protected void DoSelectedTypeChange(DmlType NewType)
        {
            if (SelectedTypeChange != null) SelectedTypeChange(NewType);
        }

        public ValueControl()
        {
            InitializeComponent();
        }
    }
}
