namespace Dml_Editor
{
    partial class TablePanel
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
            this.tabs = new System.Windows.Forms.TabControl();
            this.tabTable = new System.Windows.Forms.TabPage();
            this.label1 = new System.Windows.Forms.Label();
            this.TypeBox = new System.Windows.Forms.ComboBox();
            this.TableErrorMsg = new System.Windows.Forms.Label();
            this.DGV = new System.Windows.Forms.DataGridView();
            this.tabGraph = new System.Windows.Forms.TabPage();
            this.tabs.SuspendLayout();
            this.tabTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DGV)).BeginInit();
            this.SuspendLayout();
            // 
            // tabs
            // 
            this.tabs.Controls.Add(this.tabGraph);
            this.tabs.Controls.Add(this.tabTable);
            this.tabs.Location = new System.Drawing.Point(3, 3);
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(577, 426);
            this.tabs.TabIndex = 4;
            this.tabs.SelectedIndexChanged += new System.EventHandler(this.tabs_SelectedIndexChanged);
            // 
            // tabTable
            // 
            this.tabTable.Controls.Add(this.DGV);
            this.tabTable.Controls.Add(this.TableErrorMsg);
            this.tabTable.Controls.Add(this.TypeBox);
            this.tabTable.Controls.Add(this.label1);
            this.tabTable.Location = new System.Drawing.Point(4, 22);
            this.tabTable.Name = "tabTable";
            this.tabTable.Padding = new System.Windows.Forms.Padding(3);
            this.tabTable.Size = new System.Drawing.Size(569, 400);
            this.tabTable.TabIndex = 1;
            this.tabTable.Text = "Table";
            this.tabTable.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Column Type:";
            // 
            // TypeBox
            // 
            this.TypeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TypeBox.FormattingEnabled = true;
            this.TypeBox.Location = new System.Drawing.Point(81, 3);
            this.TypeBox.Name = "TypeBox";
            this.TypeBox.Size = new System.Drawing.Size(479, 21);
            this.TypeBox.TabIndex = 2;
            this.TypeBox.SelectedIndexChanged += new System.EventHandler(this.TypeBox_SelectedIndexChanged);
            // 
            // TableErrorMsg
            // 
            this.TableErrorMsg.AutoSize = true;
            this.TableErrorMsg.ForeColor = System.Drawing.Color.Maroon;
            this.TableErrorMsg.Location = new System.Drawing.Point(6, 384);
            this.TableErrorMsg.Name = "TableErrorMsg";
            this.TableErrorMsg.Size = new System.Drawing.Size(76, 13);
            this.TableErrorMsg.TabIndex = 3;
            this.TableErrorMsg.Text = "TableErrorMsg";
            this.TableErrorMsg.Visible = false;
            // 
            // DGV
            // 
            this.DGV.AllowUserToOrderColumns = true;
            this.DGV.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DGV.Location = new System.Drawing.Point(6, 30);
            this.DGV.Name = "DGV";
            this.DGV.Size = new System.Drawing.Size(554, 351);
            this.DGV.TabIndex = 0;
            this.DGV.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.DGV_CellValueChanged);
            this.DGV.ColumnHeaderMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.DGV_ColumnHeaderMouseDoubleClick);
            this.DGV.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.DGV_CellMouseClick);
            this.DGV.MouseClick += new System.Windows.Forms.MouseEventHandler(this.DGV_MouseClick);
            this.DGV.ColumnHeaderCellChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(this.DGV_ColumnHeaderCellChanged);
            this.DGV.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.DGV_RowsAdded);
            this.DGV.CurrentCellChanged += new System.EventHandler(this.DGV_CurrentCellChanged);
            this.DGV.KeyDown += new System.Windows.Forms.KeyEventHandler(this.DGV_KeyDown);
            this.DGV.SelectionChanged += new System.EventHandler(this.DGV_SelectionChanged);
            this.DGV.ColumnAdded += new System.Windows.Forms.DataGridViewColumnEventHandler(this.DGV_ColumnAdded);
            // 
            // tabGraph
            // 
            this.tabGraph.Location = new System.Drawing.Point(4, 22);
            this.tabGraph.Name = "tabGraph";
            this.tabGraph.Padding = new System.Windows.Forms.Padding(3);
            this.tabGraph.Size = new System.Drawing.Size(569, 400);
            this.tabGraph.TabIndex = 0;
            this.tabGraph.Text = "Graph";
            this.tabGraph.UseVisualStyleBackColor = true;
            // 
            // TablePanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabs);
            this.Name = "TablePanel";
            this.Size = new System.Drawing.Size(580, 432);
            this.Load += new System.EventHandler(this.TablePanel_Load);
            this.tabs.ResumeLayout(false);
            this.tabTable.ResumeLayout(false);
            this.tabTable.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DGV)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.TabPage tabGraph;
        private System.Windows.Forms.TabPage tabTable;
        private System.Windows.Forms.DataGridView DGV;
        private System.Windows.Forms.Label TableErrorMsg;
        private System.Windows.Forms.ComboBox TypeBox;
        private System.Windows.Forms.Label label1;


    }
}
