namespace Dml_Editor
{
    partial class PrimitivePanel
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.gbPrimitive = new System.Windows.Forms.GroupBox();
            this.rbFalseValue = new System.Windows.Forms.RadioButton();
            this.rbTrueValue = new System.Windows.Forms.RadioButton();
            this.DateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.labelValueError = new System.Windows.Forms.Label();
            this.ValueTextBox = new System.Windows.Forms.TextBox();
            this.TypeBox = new System.Windows.Forms.ComboBox();
            this.labelType = new System.Windows.Forms.Label();
            this.NameBox = new System.Windows.Forms.TextBox();
            this.labelValue = new System.Windows.Forms.Label();
            this.labelName = new System.Windows.Forms.Label();
            this.gbPrimitive.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbPrimitive
            // 
            this.gbPrimitive.Controls.Add(this.rbFalseValue);
            this.gbPrimitive.Controls.Add(this.rbTrueValue);
            this.gbPrimitive.Controls.Add(this.DateTimePicker);
            this.gbPrimitive.Controls.Add(this.labelValueError);
            this.gbPrimitive.Controls.Add(this.ValueTextBox);
            this.gbPrimitive.Controls.Add(this.TypeBox);
            this.gbPrimitive.Controls.Add(this.labelType);
            this.gbPrimitive.Controls.Add(this.NameBox);
            this.gbPrimitive.Controls.Add(this.labelValue);
            this.gbPrimitive.Controls.Add(this.labelName);
            this.gbPrimitive.Location = new System.Drawing.Point(4, 4);
            this.gbPrimitive.Name = "gbPrimitive";
            this.gbPrimitive.Size = new System.Drawing.Size(573, 425);
            this.gbPrimitive.TabIndex = 28;
            this.gbPrimitive.TabStop = false;
            this.gbPrimitive.Text = "Primitive";
            // 
            // rbFalseValue
            // 
            this.rbFalseValue.AutoSize = true;
            this.rbFalseValue.Location = new System.Drawing.Point(315, 99);
            this.rbFalseValue.Name = "rbFalseValue";
            this.rbFalseValue.Size = new System.Drawing.Size(50, 17);
            this.rbFalseValue.TabIndex = 16;
            this.rbFalseValue.TabStop = true;
            this.rbFalseValue.Text = "&False";
            this.rbFalseValue.UseVisualStyleBackColor = true;
            this.rbFalseValue.CheckedChanged += new System.EventHandler(this.rbFalseValue_CheckedChanged);
            // 
            // rbTrueValue
            // 
            this.rbTrueValue.AutoSize = true;
            this.rbTrueValue.Location = new System.Drawing.Point(231, 99);
            this.rbTrueValue.Name = "rbTrueValue";
            this.rbTrueValue.Size = new System.Drawing.Size(47, 17);
            this.rbTrueValue.TabIndex = 15;
            this.rbTrueValue.TabStop = true;
            this.rbTrueValue.Text = "&True";
            this.rbTrueValue.UseVisualStyleBackColor = true;
            this.rbTrueValue.CheckedChanged += new System.EventHandler(this.rbTrueValue_CheckedChanged);
            // 
            // DateTimePicker
            // 
            this.DateTimePicker.CustomFormat = "MMMM dd, yyyy \'at\' hh:mm:ss tt";
            this.DateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.DateTimePicker.Location = new System.Drawing.Point(24, 95);
            this.DateTimePicker.Name = "DateTimePicker";
            this.DateTimePicker.Size = new System.Drawing.Size(285, 20);
            this.DateTimePicker.TabIndex = 45;
            this.DateTimePicker.ValueChanged += new System.EventHandler(this.DateTimePicker_ValueChanged);
            // 
            // labelValueError
            // 
            this.labelValueError.AutoSize = true;
            this.labelValueError.ForeColor = System.Drawing.Color.Maroon;
            this.labelValueError.Location = new System.Drawing.Point(21, 398);
            this.labelValueError.Name = "labelValueError";
            this.labelValueError.Size = new System.Drawing.Size(78, 13);
            this.labelValueError.TabIndex = 36;
            this.labelValueError.Text = "labelValueError";
            this.labelValueError.Visible = false;
            // 
            // ValueTextBox
            // 
            this.ValueTextBox.Location = new System.Drawing.Point(24, 95);
            this.ValueTextBox.Multiline = true;
            this.ValueTextBox.Name = "ValueTextBox";
            this.ValueTextBox.Size = new System.Drawing.Size(521, 300);
            this.ValueTextBox.TabIndex = 35;
            this.ValueTextBox.Visible = false;
            this.ValueTextBox.TextChanged += new System.EventHandler(this.OnValueTextChanged);
            this.ValueTextBox.Validated += new System.EventHandler(this.ValueTextBox_Validated);
            // 
            // TypeBox
            // 
            this.TypeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TypeBox.FormattingEnabled = true;
            this.TypeBox.Location = new System.Drawing.Point(275, 36);
            this.TypeBox.Name = "TypeBox";
            this.TypeBox.Size = new System.Drawing.Size(285, 21);
            this.TypeBox.TabIndex = 32;
            this.TypeBox.SelectedIndexChanged += new System.EventHandler(this.TypeBox_SelectedIndexChanged);
            // 
            // labelType
            // 
            this.labelType.AutoSize = true;
            this.labelType.Location = new System.Drawing.Point(272, 20);
            this.labelType.Name = "labelType";
            this.labelType.Size = new System.Drawing.Size(31, 13);
            this.labelType.TabIndex = 31;
            this.labelType.Text = "Type";
            // 
            // NameBox
            // 
            this.NameBox.Location = new System.Drawing.Point(24, 37);
            this.NameBox.Name = "NameBox";
            this.NameBox.Size = new System.Drawing.Size(245, 20);
            this.NameBox.TabIndex = 30;
            this.NameBox.Validated += new System.EventHandler(this.NameBox_Validated);
            // 
            // labelValue
            // 
            this.labelValue.AutoSize = true;
            this.labelValue.Location = new System.Drawing.Point(10, 69);
            this.labelValue.Name = "labelValue";
            this.labelValue.Size = new System.Drawing.Size(34, 13);
            this.labelValue.TabIndex = 29;
            this.labelValue.Text = "Value";
            // 
            // labelName
            // 
            this.labelName.AutoSize = true;
            this.labelName.Location = new System.Drawing.Point(10, 21);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(35, 13);
            this.labelName.TabIndex = 28;
            this.labelName.Text = "Name";
            // 
            // PrimitivePanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gbPrimitive);
            this.Name = "PrimitivePanel";
            this.Size = new System.Drawing.Size(580, 432);
            this.Load += new System.EventHandler(this.PrimitivePanel_Load);
            this.gbPrimitive.ResumeLayout(false);
            this.gbPrimitive.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbPrimitive;
        public System.Windows.Forms.RadioButton rbTrueValue;
        public System.Windows.Forms.RadioButton rbFalseValue;
        private System.Windows.Forms.Label labelValueError;
        public System.Windows.Forms.TextBox ValueTextBox;
        private System.Windows.Forms.ComboBox TypeBox;
        private System.Windows.Forms.Label labelType;
        private System.Windows.Forms.TextBox NameBox;
        public System.Windows.Forms.Label labelValue;
        private System.Windows.Forms.Label labelName;
        public System.Windows.Forms.DateTimePicker DateTimePicker;
    }
}
