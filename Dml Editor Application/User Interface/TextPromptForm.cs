using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Dml_Editor.User_Interface
{
    public partial class TextPromptForm : Form
    {
        public TextPromptForm()
        {
            InitializeComponent();
        }

        private void TextPromptForm_Load(object sender, EventArgs e)
        {
            UserText.Left = PromptLabel.Right + 35;
            UserText.Width = btnCancel.Right - UserText.Left;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }
    }
}
