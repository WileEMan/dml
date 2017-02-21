using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WileyBlack.Dml;
using WileyBlack.Dml.Dom;

namespace Dml_Editor
{
    public partial class AddPrimitiveSetForm : Form
    {
        public class CodecOption
        {
            public DomPrimitiveSet Set;
            public string Label;

            public CodecOption(DomPrimitiveSet Set, string Label)
            {
                this.Set = Set;
                this.Label = Label;
            }

            public override string ToString()
            {
                return Label;
            }
        }

        public AddPrimitiveSetForm()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
