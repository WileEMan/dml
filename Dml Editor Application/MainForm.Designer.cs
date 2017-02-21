namespace Dml_Editor
{
    partial class MainForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.DocTree = new System.Windows.Forms.TreeView();
            this.MainMenu = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.importXmlToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportXmlToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hideRecognizedDetailsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showDiagnosticInformationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.namespaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.namespaceToCClassToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.defaultDMLProgramToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.defaultDVidProgramToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.clearResourceCacheToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.ElementList = new System.Windows.Forms.ListBox();
            this.AttrList = new System.Windows.Forms.ListBox();
            this.lblStatusBar = new System.Windows.Forms.Label();
            this.translationToCClassToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MainMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // DocTree
            // 
            this.DocTree.LabelEdit = true;
            this.DocTree.Location = new System.Drawing.Point(12, 43);
            this.DocTree.Name = "DocTree";
            this.DocTree.Size = new System.Drawing.Size(301, 432);
            this.DocTree.TabIndex = 0;
            this.DocTree.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.DocTree_AfterLabelEdit);
            this.DocTree.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.DocTree_BeforeExpand);
            this.DocTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.DocTree_AfterSelect);
            this.DocTree.KeyUp += new System.Windows.Forms.KeyEventHandler(this.DocTree_KeyUp);
            this.DocTree.MouseUp += new System.Windows.Forms.MouseEventHandler(this.DocTree_MouseUp);
            // 
            // MainMenu
            // 
            this.MainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.namespaceToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.MainMenu.Location = new System.Drawing.Point(0, 0);
            this.MainMenu.Name = "MainMenu";
            this.MainMenu.Size = new System.Drawing.Size(958, 24);
            this.MainMenu.TabIndex = 1;
            this.MainMenu.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.closeToolStripMenuItem,
            this.toolStripSeparator1,
            this.importXmlToolStripMenuItem,
            this.exportXmlToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.newToolStripMenuItem.Text = "&New";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.openToolStripMenuItem.Text = "&Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.saveAsToolStripMenuItem.Text = "Save &As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.closeToolStripMenuItem.Text = "&Close";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(140, 6);
            // 
            // importXmlToolStripMenuItem
            // 
            this.importXmlToolStripMenuItem.Name = "importXmlToolStripMenuItem";
            this.importXmlToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.importXmlToolStripMenuItem.Text = "&Import Xml...";
            this.importXmlToolStripMenuItem.Click += new System.EventHandler(this.importXmlToolStripMenuItem_Click);
            // 
            // exportXmlToolStripMenuItem
            // 
            this.exportXmlToolStripMenuItem.Name = "exportXmlToolStripMenuItem";
            this.exportXmlToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.exportXmlToolStripMenuItem.Text = "Export &Xml...";
            this.exportXmlToolStripMenuItem.Click += new System.EventHandler(this.exportXmlToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.hideRecognizedDetailsToolStripMenuItem,
            this.showDiagnosticInformationToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "&View";
            // 
            // hideRecognizedDetailsToolStripMenuItem
            // 
            this.hideRecognizedDetailsToolStripMenuItem.Checked = true;
            this.hideRecognizedDetailsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.hideRecognizedDetailsToolStripMenuItem.Enabled = false;
            this.hideRecognizedDetailsToolStripMenuItem.Name = "hideRecognizedDetailsToolStripMenuItem";
            this.hideRecognizedDetailsToolStripMenuItem.Size = new System.Drawing.Size(228, 22);
            this.hideRecognizedDetailsToolStripMenuItem.Text = "Hide Recognized &Details";
            this.hideRecognizedDetailsToolStripMenuItem.Click += new System.EventHandler(this.hideRecognizedDetailsToolStripMenuItem_Click);
            // 
            // showDiagnosticInformationToolStripMenuItem
            // 
            this.showDiagnosticInformationToolStripMenuItem.Name = "showDiagnosticInformationToolStripMenuItem";
            this.showDiagnosticInformationToolStripMenuItem.Size = new System.Drawing.Size(228, 22);
            this.showDiagnosticInformationToolStripMenuItem.Text = "&Show Diagnostic Information";
            this.showDiagnosticInformationToolStripMenuItem.Click += new System.EventHandler(this.showDiagnosticInformationToolStripMenuItem_Click);
            // 
            // namespaceToolStripMenuItem
            // 
            this.namespaceToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.namespaceToCClassToolStripMenuItem,
            this.translationToCClassToolStripMenuItem});
            this.namespaceToolStripMenuItem.Name = "namespaceToolStripMenuItem";
            this.namespaceToolStripMenuItem.Size = new System.Drawing.Size(77, 20);
            this.namespaceToolStripMenuItem.Text = "&Translation";
            // 
            // namespaceToCClassToolStripMenuItem
            // 
            this.namespaceToCClassToolStripMenuItem.Name = "namespaceToCClassToolStripMenuItem";
            this.namespaceToCClassToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
            this.namespaceToCClassToolStripMenuItem.Text = "Translation  to &C# Class";
            this.namespaceToCClassToolStripMenuItem.Click += new System.EventHandler(this.translationToCClassToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.defaultDMLProgramToolStripMenuItem,
            this.defaultDVidProgramToolStripMenuItem,
            this.toolStripSeparator2,
            this.clearResourceCacheToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "&Options";
            // 
            // defaultDMLProgramToolStripMenuItem
            // 
            this.defaultDMLProgramToolStripMenuItem.Name = "defaultDMLProgramToolStripMenuItem";
            this.defaultDMLProgramToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.defaultDMLProgramToolStripMenuItem.Text = "Default DML Program...";
            this.defaultDMLProgramToolStripMenuItem.Click += new System.EventHandler(this.defaultDMLProgramToolStripMenuItem_Click);
            // 
            // defaultDVidProgramToolStripMenuItem
            // 
            this.defaultDVidProgramToolStripMenuItem.Name = "defaultDVidProgramToolStripMenuItem";
            this.defaultDVidProgramToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.defaultDVidProgramToolStripMenuItem.Text = "Default d&Vid Program...";
            this.defaultDVidProgramToolStripMenuItem.Click += new System.EventHandler(this.defaultDVidProgramToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(195, 6);
            // 
            // clearResourceCacheToolStripMenuItem
            // 
            this.clearResourceCacheToolStripMenuItem.Name = "clearResourceCacheToolStripMenuItem";
            this.clearResourceCacheToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.clearResourceCacheToolStripMenuItem.Text = "&Clear Resource Cache";
            this.clearResourceCacheToolStripMenuItem.Click += new System.EventHandler(this.clearResourceCacheToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.aboutToolStripMenuItem.Text = "&About...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(319, 43);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Attributes";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(319, 273);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Elements";
            // 
            // ElementList
            // 
            this.ElementList.FormattingEnabled = true;
            this.ElementList.Location = new System.Drawing.Point(319, 289);
            this.ElementList.Name = "ElementList";
            this.ElementList.Size = new System.Drawing.Size(323, 186);
            this.ElementList.TabIndex = 3;
            this.ElementList.SelectedIndexChanged += new System.EventHandler(this.ElementList_SelectedIndexChanged);
            // 
            // AttrList
            // 
            this.AttrList.FormattingEnabled = true;
            this.AttrList.Location = new System.Drawing.Point(319, 59);
            this.AttrList.Name = "AttrList";
            this.AttrList.Size = new System.Drawing.Size(323, 186);
            this.AttrList.TabIndex = 2;
            this.AttrList.SelectedIndexChanged += new System.EventHandler(this.AttrList_SelectedIndexChanged);
            this.AttrList.KeyUp += new System.Windows.Forms.KeyEventHandler(this.AttrList_KeyUp);
            // 
            // lblStatusBar
            // 
            this.lblStatusBar.AutoSize = true;
            this.lblStatusBar.Location = new System.Drawing.Point(13, 482);
            this.lblStatusBar.Name = "lblStatusBar";
            this.lblStatusBar.Size = new System.Drawing.Size(63, 13);
            this.lblStatusBar.TabIndex = 6;
            this.lblStatusBar.Text = "lblStatusBar";
            // 
            // translationToCClassToolStripMenuItem
            // 
            this.translationToCClassToolStripMenuItem.Name = "translationToCClassToolStripMenuItem";
            this.translationToCClassToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
            this.translationToCClassToolStripMenuItem.Text = "Translation to C&++ Class";
            this.translationToCClassToolStripMenuItem.Click += new System.EventHandler(this.translationToCClassToolStripMenuItem_Click_1);
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(958, 503);
            this.Controls.Add(this.lblStatusBar);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ElementList);
            this.Controls.Add(this.AttrList);
            this.Controls.Add(this.DocTree);
            this.Controls.Add(this.MainMenu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.MainMenu;
            this.Name = "MainForm";
            this.Text = "Dml Editor";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
            this.MainMenu.ResumeLayout(false);
            this.MainMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView DocTree;
        private System.Windows.Forms.MenuStrip MainMenu;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ListBox AttrList;
        private System.Windows.Forms.ListBox ElementList;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hideRecognizedDetailsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem importXmlToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportXmlToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem namespaceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem namespaceToCClassToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem defaultDMLProgramToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem defaultDVidProgramToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showDiagnosticInformationToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem clearResourceCacheToolStripMenuItem;
        private System.Windows.Forms.Label lblStatusBar;
        private System.Windows.Forms.ToolStripMenuItem translationToCClassToolStripMenuItem;
    }
}

