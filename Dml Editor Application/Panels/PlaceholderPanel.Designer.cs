namespace Dml_Editor
{
    partial class PlaceholderPanel
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
            this.gbPlaceholder = new System.Windows.Forms.GroupBox();
            this.labelName = new System.Windows.Forms.Label();
            this.gbPlaceholder.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbPlaceholder
            // 
            this.gbPlaceholder.Controls.Add(this.labelName);
            this.gbPlaceholder.Location = new System.Drawing.Point(4, 4);
            this.gbPlaceholder.Name = "gbPlaceholder";
            this.gbPlaceholder.Size = new System.Drawing.Size(573, 425);
            this.gbPlaceholder.TabIndex = 28;
            this.gbPlaceholder.TabStop = false;
            this.gbPlaceholder.Text = "Selection";
            // 
            // labelName
            // 
            this.labelName.AutoSize = true;
            this.labelName.Location = new System.Drawing.Point(242, 206);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(89, 13);
            this.labelName.TabIndex = 28;
            this.labelName.Text = "No item selected.";
            // 
            // PlaceholderPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gbPlaceholder);
            this.Name = "PlaceholderPanel";
            this.Size = new System.Drawing.Size(580, 432);
            this.gbPlaceholder.ResumeLayout(false);
            this.gbPlaceholder.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbPlaceholder;
        private System.Windows.Forms.Label labelName;
    }
}
