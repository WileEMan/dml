/** Dml Editor
 * 
 *  The MainForm is the top-level of the application.  It contains three "panes":  Left (Tree), Middle (Attributes & Elements), and
 *  Right.  The Rightmost Pane is empty on MainForm, the space is reserved for various Panels which are inserted when appropriate.
 *  
 *  Selections come in two pieces: Primary and Secondary.  The Secondary selection is always in the Left pane, a DML Container.  The
 *  Primary selection can come from multiple sources, but the simplest is to select an attribute or a primitive element in the middle
 *  pane.  The Primary selection dictates which panel is displayed in the rightmost pane.  Primary selections also happen when 
 *  "recognized" containers are selected in the leftmost pane.  
 * 
 *  The DmlNodeInfo class and its derivates retain ancillary information about Dml nodes which is not stored in Dml.  For example,
 *  a DmlTableInfo organizes the information from a "Table" Dml container in such a way that the GUI can interact with it.  The
 *  DmlPrimitiveInfo class remembers what selection the user had for the "type" drop-down box for the primitive.  This information
 *  also exists in which type of DmlPrimitive object represents the primitive, but the DmlPrimitiveInfo class supports an automatic
 *  option as well.
 *  
 *  The DmlType class and its descendents provide several abstract behaviors which specific types implement.  For example, a 
 *  DmlDoubleType might know how to perform conversion to/from DmlDouble objects.
 */

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
using Microsoft.Win32;
using Dml_Editor.Conversion;
using Dml_Editor.User_Interface;

namespace Dml_Editor
{
    public partial class MainForm : Form, IResourceResolution
    {
        #region "Properties"

        DmlNodeInfo m_SelectedPrimary;

        /// <summary>
        /// There are two selections possible at a time in the MainForm window.  The "primary" selection is the
        /// most detailed, value selection.  The "secondary" selection is the container node which is presently
        /// selected in the leftmost tree pane.  Whenever a primary selection is selected, it gains precedence in
        /// the rightmost pane.  When no primary selection is selected, the rightmost pane is either empty, or
        /// can be populated by a higher-level display of the secondary selection, such as an image constructed
        /// from the secondary node and its children.
        /// </summary>        
        DmlNodeInfo SelectedPrimary
        {
            get { return m_SelectedPrimary; }
            set
            {
                m_SelectedPrimary = value;
                LoadPrimary();

                if (m_SelectedPrimary != null)
                {
                    string str = "Selected node '" + m_SelectedPrimary.Node.Name + "' encoded ";
                    if (m_SelectedPrimary.Node.InlineIdentification) str = str + "with inline identification.";
                    else str = str + "using DMLID " + m_SelectedPrimary.Node.ID + ".";
                    lblStatusBar.Text = str;
                }
            }
        }

        DmlNodeInfo SelectedSecondary
        {
            get
            {
                if (DocTree.SelectedNode != null) return (DmlNodeInfo)DocTree.SelectedNode.Tag;
                return null;
            }
        }

        DmlContainer SelectedContainer
        {
            get
            {
                if (SelectedSecondary == null) return null;
                return (SelectedSecondary as DmlContainerInfo).Container;
            }
        }

        DmlFragmentInfo SelectedContainerInfo
        {
            get
            {
                if (SelectedSecondary == null) return null;
                return (SelectedSecondary as DmlFragmentInfo);
            }
        }

        #endregion        

        #region "Form-level UI"

        public MainForm()
        {
            try
            {
                InitializeComponent();
                InitPrimaryPanels();
                newToolStripMenuItem_Click(null, null);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); Close(); }
        }

        DmlDocument Document;

        /// <summary>
        /// Extensions can be added here and ParsingOptions will be provided anytime we create a new
        /// DmlReader or are parsing an external header document.
        /// </summary>
        ParsingOptions ParsingOptions = new ParsingOptions();

        /// <summary>
        /// Reader and CurrentStream are stored if the current document is being read from a file.  This 
        /// is necessary because the DOM does not load the entire file contents when LoadPartial() is 
        /// used, and nodes must be loaded on-demand.  Reader becomes null when the user starts a new 
        /// document or saves a document.
        /// </summary>
        DmlReader Reader = null;
        Stream CurrentStream = null;

        public static string AppKey = @"HKEY_CURRENT_USER\Software\Wiley Black's Software\Dml Editor";
        public static string RelAppKey = @"Software\Wiley Black's Software\Dml Editor";
        public static string HandlerName = "Dml.WBDmlEditor";        
        public static string FileTypeDescription = "Data Markup Language File";
        public static string VidHandlerName = "dVid.WBDmlEditor";
        public static string VidFileTypeDescription = "DML Video File";

        private void MainForm_Load(object sender, EventArgs e)
        {
            lblStatusBar.Text = "Loading...";

            LoadPrimary();

            string ExecutablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (!Utility.IsAssociated(".dml", ExecutablePath))
            {
                object obj = Registry.GetValue(AppKey, "DoNotAssociate", (int)0);
                bool AlreadyAsked = false;                
                if (obj is int && (int)obj != 0) AlreadyAsked = true;
                int ii;
                if (obj is string && int.TryParse((string)obj, out ii) && ii != 0) AlreadyAsked = true;
                if (!AlreadyAsked)
                {
                    if (MessageBox.Show("Dml Editor is not currently associated with .dml files.  Would you like to make Dml Editor your default "
                        + "dml editor?", "Default Program", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        Registry.SetValue(AppKey, "DoNotAssociate", (uint)0);
                        Utility.SetAssociation(".dml", HandlerName, ExecutablePath, FileTypeDescription);
                    }
                    else Registry.SetValue(AppKey, "DoNotAssociate", (uint)1);
                }
            }
            else Registry.SetValue(AppKey, "DoNotAssociate", (uint)0);

            lblStatusBar.Text = "";
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                CloseFile();                

                Document = new DmlDocument();                
                Document.Name = "Unnamed";

                DocumentToUI(false);
            }
//            catch (Exception ex) { MessageBox.Show(ex.Message); Close(); }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); Close(); }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseFile();
        }

        void CloseFile()
        {
            if (Reader != null) { Reader.Dispose(); Reader = null; }
            if (CurrentStream != null) { CurrentStream.Dispose(); CurrentStream = null; }
            Document = null;
        }        

        public void OpenFile(string Path)
        {
            CurrentStream = new FileStream(Path, FileMode.Open);            
            Document = new DmlDocument();
            Reader = DmlReader.Create(CurrentStream, ParsingOptions);

            Exception refire = null;
            try
            {
                Document.LoadPartial(Reader, DmlFragment.LoadState.Full, this);
            }
            catch (Exception ex) { refire = ex; }

            // Load one additional level...
            try
            {
                foreach (DmlNode Node in Document.Children)
                {
                    if (Node is DmlFragment && ((DmlFragment)Node).Loaded != DmlFragment.LoadState.Full)
                    {
                        try
                        {
                            ((DmlFragment)Node).LoadPartial(Reader, DmlFragment.LoadState.Full);
                        }
                        catch (Exception ex)
                        {
                            if (refire == null) refire = ex;
                        }
                    }
                }
            }
            catch (Exception ex) 
            { 
                // This exception is usually a result of the first one and isn't real itself...
                if (refire == null) refire = ex;
            }

            try
            {
                DocumentToUI(true);
            }
            catch (Exception ex)
            {
                if (refire == null) refire = ex;
            }

            if (refire != null) throw refire;
        }

        public void OpenXmlFile(string Path)
        {
            CloseFile();            

            Document = XmlToDml.ImportXml(Path, this);
            DocumentToUI(true);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Dml Files (*.dml, *.dVid)|*.dml;*.dVid|All Files (*.*)|*.*";
                if (ofd.ShowDialog() != DialogResult.OK) return;
                OpenFile(ofd.FileName);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void importXmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Xml Files (*.xml)|*.xml|All Files (*.*)|*.*";
                if (ofd.ShowDialog() != DialogResult.OK) return;
                OpenXmlFile(ofd.FileName);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Dml Files (*.dml, *.dVid)|*.dml;*.dVid|All Files (*.*)|*.*";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                if (!Document.IsFullyLoaded) Document.LoadContent(Reader);
                if (Reader != null) { Reader.Dispose(); Reader = null; }
                if (CurrentStream != null) { CurrentStream.Dispose(); CurrentStream = null; }

                Document.SaveToFile(sfd.FileName, true);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void exportXmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Xml Files (*.xml)|*.xml|All Files (*.*)|*.*";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                if (!Document.IsFullyLoaded) Document.LoadContent(Reader);
                if (Reader != null) { Reader.Dispose(); Reader = null; }
                if (CurrentStream != null) { CurrentStream.Dispose(); CurrentStream = null; }

                DmlToXml.SaveToXml(Document, sfd.FileName);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true) e.Effect = DragDropEffects.All;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Exception LastError = null;
            foreach (string Filename in files)
            {
                try
                {
                    if (Path.GetExtension(Filename).ToLower() == ".dml") OpenFile(Filename);
                    else if (Path.GetExtension(Filename).ToLower() == ".dvid") OpenFile(Filename);
                    else if (Path.GetExtension(Filename).ToLower() == ".xml") OpenXmlFile(Filename);
                    else throw new Exception("File extension '" + Path.GetExtension(Filename) + "' is not recognized by Dml Editor.");
                }
                catch (Exception ex) { LastError = ex; }
            }
            if (LastError != null) MessageBox.Show(LastError.Message);
        }

        #endregion

        #region "Left-pane (DocTree) UI"

        void DocumentToUI(bool FromFile)
        {
            DocTree.Nodes.Clear();
            DocTree.Nodes.Add(DmlToTree(Document.Header));
            TreeNode Root = DmlToTree(Document);
            DocTree.Nodes.Add(Root);
            DocTree.SelectedNode = Root;

            //DoRecognizedDetails();
        }

        TreeNode DmlToTree(DmlNode Node, DmlFragmentInfo ContainerInfo)
        {
            if (Node is DmlCompressed)
            {
                DmlCompressed CompressedNode = (DmlCompressed)Node;
                TreeNode ret = new TreeNode(Node.Name);
                DmlCompressedInfo dci = new DmlCompressedInfo(CompressedNode, ContainerInfo);
                ret.Tag = dci;

                // DmlCompressed nodes do not contain a size indicator.  While the
                // compressed node's parent might, the DmlCompressed itself must
                // be decompressed in its entirety.  This is by design, since some
                // writers may not be able to know compressed size in advance.  
                 
                // The current DML EC DOM design parses the DML as it decompresses 
                // it, so there is very little value in a partial load at this point 
                // (only the UI Tree need be created.)  So, we unpack the fragment 
                // completely.

                foreach (DmlNode child in CompressedNode.DecompressedFragment.Children)
                {
                    TreeNode tnd = DmlToTree(child, dci);
                    if (tnd != null)
                    {
                        DmlNodeInfo dni = (DmlNodeInfo)tnd.Tag;
                        dci.Children.Add(dni);
                        ret.Nodes.Add(tnd);
                    }
                }
                return ret;
            }

            if (Node is DmlContainer)
            {
                if (((DmlContainer)Node).Loaded != DmlFragment.LoadState.Full) return null;

                TreeNode ret = new TreeNode(Node.Name);

                DmlContainer Container = Node as DmlContainer;
                if (Container.Name == "Table")
                {
                    DmlTableInfo dti = new DmlTableInfo(Container, ContainerInfo);
                    ret.Tag = dti;                    

                    /** if not (Hide Recognized Details)
                    foreach (DmlNode child in Container.Children)
                        ret.Nodes.Add(DmlToTree(child, AutomaticTypes));
                     */
                }
                else if (Container.Name == "Image")
                {
                    DmlImageInfo dii = new DmlImageInfo(Container, ContainerInfo);
                    ret.Tag = dii;
                }
                else
                {
                    DmlContainerInfo dci = new DmlContainerInfo(Container, ContainerInfo);
                    ret.Tag = dci;
                    foreach (DmlPrimitive attr in Container.Attributes)
                    {
                        DmlPrimitiveInfo dpi = new DmlPrimitiveInfo(attr, dci);
                        dci.Attributes.Add(dpi);
                    }
                    foreach (DmlNode child in Container.Children)
                    {
                        TreeNode tnd = DmlToTree(child, dci);
                        if (tnd != null)
                        {
                            DmlNodeInfo dni = (DmlNodeInfo)tnd.Tag;
                            dci.Children.Add(dni);

                            ret.Nodes.Add(tnd);
                        }
                        else
                        {
                            // The node does not get a spot on the tree, but primitives should still get
                            // listed as an element...
                            if (child is DmlPrimitive)
                            {
                                DmlPrimitiveInfo dpi = new DmlPrimitiveInfo((DmlPrimitive)child, dci);
                                dci.Children.Add(dpi);
                            }
                        }
                    }
                }
                return ret;
            }
            return null;
        }

        TreeNode DmlToTree(DmlNode Node)
        {
            return DmlToTree(Node, null);
        }

#       if false
        void DoRecognizedDetails()
        {
            foreach (TreeNode Node in DocTree.Nodes) DoRecognizedDetails(Node);
            BuildAttrList();
            BuildElementList();
        }

        void DoRecognizedDetails(TreeNode Node)
        {
            if (Node.Tag is DmlTableInfo)
            {
            }

            foreach (TreeNode ChildNode in DocTree.Nodes) DoRecognizedDetails(ChildNode);
        }
#       endif
        bool IsRecognizedDetail(DmlNode Detail)
        {
            if (!HideRecognizedDetails) return true;

            if (SelectedSecondary is DmlContainerInfo)
                return ((DmlContainerInfo)SelectedSecondary).IsRecognizedDetail(Detail);
            return false;
        }

        private void DocTree_KeyUp(object sender, KeyEventArgs e)
        {            
            try
            {
                switch (e.KeyCode)
                {
                    case Keys.Insert:
                        {
                            if (SelectedContainer == null) return;
                            DmlContainer NewChild = new DmlContainer(SelectedContainer);
                            NewChild.Name = "Unnamed";
                            SelectedContainer.Children.Add(NewChild);
                            DocTree.SelectedNode.Nodes.Add(DmlToTree(NewChild, SelectedContainerInfo));
                            break;
                        }

                    case Keys.Delete:
                        {
                            if (SelectedContainer == null) return;
                            DmlFragment Container = SelectedContainer.Container;
                            Container.Children.Remove(SelectedContainer);
                            DocTree.SelectedNode.Remove();
                            break;
                        }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }        

        private void DocTree_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            try
            {
                if (e.Label == null) return;
                if (e.Node.Tag is DmlContainerInfo)
                {
                    ((DmlContainerInfo)e.Node.Tag).Container.Name = e.Label;
                    e.Node.Name = e.Label;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void DocTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                BuildAttrList();
                BuildElementList();

                if (SelectedSecondary is DmlTableInfo) SelectedPrimary = SelectedSecondary;
                else if (SelectedSecondary is DmlImageInfo) SelectedPrimary = SelectedSecondary;
                else SelectedPrimary = null;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void DocTree_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button != MouseButtons.Right) return;
                TreeNode ClickedTreeNode = DocTree.GetNodeAt(new Point(e.X, e.Y));
                if (ClickedTreeNode == null) return;
                if (ClickedTreeNode != DocTree.SelectedNode)
                {                
                    // Setting SelectedNode will invoke DocTree_AfterSelect(), which will update the
                    // attribute list, element list, and in turn, SelectedPrimary.
                    DocTree.SelectedNode = ClickedTreeNode;
                }
                DmlNodeInfo ClickedNode = ClickedTreeNode.Tag as DmlNodeInfo;
                if (ClickedNode is DmlFragmentInfo)
                {
                    ContextMenu Menu = new ContextMenu();
                    if (ClickedNode is DmlContainerInfo)
                        Menu.MenuItems.Add(new MenuItem("Add &Attribute", new EventHandler(OnAddAttribute)));
                    Menu.MenuItems.Add(new MenuItem("Add &Element", new EventHandler(OnAddElement)));
                    Menu.MenuItems.Add(new MenuItem("Add &Container", new EventHandler(OnAddContainer)));
                    if (!(ClickedNode is DmlImageInfo) && !(ClickedNode is DmlTableInfo))
                    {
                        Menu.MenuItems.Add("-");
                        Menu.MenuItems.Add(new MenuItem("Add &Compression", new EventHandler(OnAddCompression)));
                        Menu.MenuItems.Add("-");
                        Menu.MenuItems.Add(new MenuItem("Add &Table", new EventHandler(OnAddTable)));
                        if (!ClickedNode.IsCompressed)
                            Menu.MenuItems.Add(new MenuItem("Add Compressed &Table", new EventHandler(OnAddCompressedTable)));
                        Menu.MenuItems.Add(new MenuItem("Add &Image", new EventHandler(OnAddImage)));                        
                        if (!ClickedNode.IsCompressed)
                            Menu.MenuItems.Add(new MenuItem("Add Lossless-Compressed &Image", new EventHandler(OnAddCompressedImage)));                        
                    }
                    Menu.Show(DocTree, new Point(e.X, e.Y));
                    return;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void DocTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            // We can expect that the children at the level being expanded are all loaded (an exception will
            // be thrown if not).  We must ensure that one additional level is loaded so that all + indicators
            // can be displayed - and so that further expansion continues to meet the expectation that any
            // clickable item has one level of children loaded.

            Exception refire = null;

            for (int ii=0; ii < e.Node.Nodes.Count; ii++)            
            {
                TreeNode TNode = e.Node.Nodes[ii];
                if (TNode.Tag is DmlContainerInfo)
                {
                    DmlContainerInfo CInfo = (DmlContainerInfo)TNode.Tag;
                    if (CInfo.Container.Loaded != DmlFragment.LoadState.Full)
                        throw new Exception("Expected current and 1 additional level to be fully loaded.");                    

                    bool Changed = false;
                    foreach (DmlNode Node in CInfo.Container.Children)
                    {
                        if (Node is DmlFragment && ((DmlFragment)Node).Loaded != DmlFragment.LoadState.Full)
                        {
                            Changed = true;
                            try
                            {
                                ((DmlFragment)Node).LoadPartial(Reader, DmlFragment.LoadState.Full);
                            }
                            catch (Exception ex) { if (refire == null) refire = ex; }
                        }
                    }
                    
                    if (Changed)
                    {
                        // One or more of the children of this node was loaded.  Remove and replace this
                        // node in the tree with the newly loaded version.

                        TreeNode NewTNode = DmlToTree(CInfo.Container, CInfo.ContainerInfo);
                        e.Node.Nodes.RemoveAt(ii);
                        e.Node.Nodes.Insert(ii, NewTNode);
                    }
                }
            }

            if (refire != null) MessageBox.Show(refire.Message);
        }

        void OnAddAttribute(object sender, EventArgs ea) 
        { 
            try
            {
                DmlContainerInfo SelectedContainerInfo = this.SelectedContainerInfo as DmlContainerInfo;
                if (SelectedContainerInfo == null) return;                

                DmlString ds = new DmlString(SelectedContainer.Document);
                ds.Name = "Unnamed";
                ds.Value = "";
                SelectedContainer.Attributes.Add(ds);
                DmlPrimitiveInfo dpi = new DmlPrimitiveInfo(ds, SelectedContainerInfo);
                SelectedContainerInfo.Attributes.Add(dpi);
                AttrList.Items.Add(dpi);
                // Setting SelectedItem triggers the selection event, which takes care of updating SelectedPrimary...
                AttrList.SelectedItem = dpi;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        void OnAddElement(object sender, EventArgs ea)
        {
            try
            {
                if (SelectedContainerInfo == null) return;

                DmlString ds = new DmlString(SelectedContainer.Document);
                ds.Name = "Unnamed";
                ds.Value = "";
                SelectedContainer.Children.Add(ds);
                DmlPrimitiveInfo dpi = new DmlPrimitiveInfo(ds, SelectedContainerInfo);
                SelectedContainerInfo.Children.Add(dpi);
                ElementList.Items.Add(dpi);
                // Setting SelectedItem triggers the selection event, which takes care of updating SelectedPrimary...
                ElementList.SelectedItem = dpi;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        void OnAddContainer(object sender, EventArgs ea)
        {
            try
            {
                DmlContainer dc = new DmlContainer(SelectedContainer.Document);
                dc.Name = "Unnamed";
                SelectedContainer.Children.Add(dc);
                TreeNode NewTreeNode = DmlToTree(dc, SelectedContainerInfo);
                SelectedContainerInfo.Children.Add((DmlContainerInfo)NewTreeNode.Tag);
                DocTree.SelectedNode.Nodes.Add(NewTreeNode);
                DocTree.SelectedNode.Expand();
                // Setting SelectedNode triggers the selection event, which takes care of updating SelectedPrimary...
                DocTree.SelectedNode = NewTreeNode;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        void OnAddCompression(object sender, EventArgs ea)
        {
            try
            {
                if (!IsDefined(SelectedContainer, DmlTranslation.EC2.Compressed.DMLName))
                {
                    if (!RequestTranslation(WileyBlack.Dml.EC.EC2Translation.urn)) return;
                }
                
                DmlCompressed dc = new DmlCompressed(SelectedContainer.Document);
                SelectedContainer.Children.Add(dc);
                TreeNode NewTreeNode = DmlToTree(dc, SelectedContainerInfo);
                SelectedContainerInfo.Children.Add((DmlCompressedInfo)NewTreeNode.Tag);
                DocTree.SelectedNode.Nodes.Add(NewTreeNode);
                DocTree.SelectedNode.Expand();
                // Setting SelectedNode triggers the selection event, which takes care of updating SelectedPrimary...
                DocTree.SelectedNode = NewTreeNode;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        void OnAddTable(object sender, EventArgs ea) { OnAddTable(false); }
        void OnAddCompressedTable(object sender, EventArgs ea) { OnAddTable(true); }

        void OnAddTable(bool WithCompression)
        {
            try
            {
                if (!IsPrimitiveSetAndCodecLoaded("arrays"))
                {
                    if (!RequestPrimitiveSetAndCodec("arrays")) return;
                }

                DmlContainer dc = new DmlContainer(SelectedContainer.Document);
                dc.Name = "Table";
                
                if (WithCompression)
                {
                    if (!IsDefined(SelectedContainer, DmlTranslation.EC2.Compressed.DMLName))
                    {
                        if (!RequestTranslation(WileyBlack.Dml.EC.EC2Translation.urn)) return;
                    }

                    // Adding a DmlCompressed child, even if empty, will trigger the AddCompression boolean
                    // when DmlTableInfo reads it.
                    dc.Children.Add(new DmlCompressed(SelectedContainer.Document));
                }

                SelectedContainer.Children.Add(dc);
                TreeNode NewTreeNode = DmlToTree(dc, SelectedContainerInfo);
                SelectedContainerInfo.Children.Add((DmlContainerInfo)NewTreeNode.Tag);
                DocTree.SelectedNode.Nodes.Add(NewTreeNode);
                DocTree.SelectedNode.Expand();
                DocTree.SelectedNode = NewTreeNode;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        void OnAddImage(object sender, EventArgs ea) { OnAddImage(false); }
        void OnAddCompressedImage(object sender, EventArgs ea) { OnAddImage(true); }

        void OnAddImage(bool WithCompression)
        {
            try
            {
                if (!IsPrimitiveSetAndCodecLoaded("arrays"))
                {
                    if (!RequestPrimitiveSetAndCodec("arrays")) return;
                }

                DmlContainer dc = new DmlContainer(SelectedContainer.Document);
                dc.Name = "Image";

                if (WithCompression)
                {
                    if (!IsDefined(SelectedContainer, DmlTranslation.EC2.Compressed.DMLName))
                    {
                        if (!RequestTranslation(WileyBlack.Dml.EC.EC2Translation.urn)) return;
                    }

                    // Adding a DmlCompressed child, even if empty, will trigger the AddCompression boolean
                    // when DmlImageInfo reads it.
                    dc.Children.Add(new DmlCompressed(SelectedContainer.Document));
                }
                
                SelectedContainer.Children.Add(dc);
                TreeNode NewTreeNode = DmlToTree(dc, SelectedContainerInfo);
                SelectedContainerInfo.Children.Add((DmlContainerInfo)NewTreeNode.Tag);
                DocTree.SelectedNode.Nodes.Add(NewTreeNode);
                DocTree.SelectedNode.Expand();
                DocTree.SelectedNode = NewTreeNode;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        bool OnEnsurePrimitiveSet(string PrimitiveSet)
        {
            if (IsPrimitiveSetAndCodecLoaded(PrimitiveSet)) return true;            
            return RequestPrimitiveSetAndCodec(PrimitiveSet);
        }

        bool IsPrimitiveSetAndCodecLoaded(string Primitives)
        {            
            foreach (PrimitiveSet Possible in Document.ResolvedHeader.PrimitiveSets)
            {
                if (Possible.Set == Primitives) return true;
            }
            return false;
        }
        
        bool RequestPrimitiveSetAndCodec(string Primitives)
        {
            try
            {
                // Prompt user for permission to add a primitive set, and ask which codec they want.
                AddPrimitiveSetForm AddForm = new AddPrimitiveSetForm();
                AddForm.labelPrimitives.Text = Primitives;
                switch (Primitives)
                {
                    case "common":
                        AddForm.BoxCodecs.Items.Add(new AddPrimitiveSetForm.CodecOption(new DomPrimitiveSet(Primitives, "le"), "Little-Endian (le) Codec"));
                        AddForm.BoxCodecs.Items.Add(new AddPrimitiveSetForm.CodecOption(new DomPrimitiveSet(Primitives, "be"), "Big-Endian (be) Codec"));
                        break;
                    case "arrays":
                        AddForm.BoxCodecs.Items.Add(new AddPrimitiveSetForm.CodecOption(new DomPrimitiveSet(Primitives, "le"), "Little-Endian (le) Codec"));
                        AddForm.BoxCodecs.Items.Add(new AddPrimitiveSetForm.CodecOption(new DomPrimitiveSet(Primitives, "be"), "Big-Endian (be) Codec"));
                        break;
                    default:
                        MessageBox.Show("An unsupported primitive set is required.");
                        return false;
                }
                AddForm.BoxCodecs.SelectedIndex = 0;
                if (AddForm.ShowDialog() != DialogResult.OK) return false;
                AddPrimitiveSetForm.CodecOption co = AddForm.BoxCodecs.SelectedItem as AddPrimitiveSetForm.CodecOption;
                if (co == null) return false;            

                // Add the codec to the document.
                Document.Header.AddPrimitiveSet(co.Set);
                RefreshDmlHeader();
                return true;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); return false; }
        }

        bool IsDefined(DmlContainer InContext, DmlName Requested)
        {
            Association Result;
            return InContext.ActiveTranslation.TryFind(Requested, out Result);
        }

        bool NeedReparseForPrimitiveSets(List<PrimitiveSet> ExistingPrimitiveSets, List<PrimitiveSet> NewPrimitiveSets)
        {
            for (int ii = 0; ii < NewPrimitiveSets.Count; ii++)
            {
                for (int jj = 0; jj < ExistingPrimitiveSets.Count; jj++)
                {
                    if (NewPrimitiveSets[ii].Set.ToLower() == ExistingPrimitiveSets[jj].Set.ToLower()) return true;
                }
            }
            return false;
        }

        bool IsXmlRootConflict(DmlContainer ExistingXmlRoot, DmlContainer NewXmlRoot)
        {
            for (int ii = 0; ii < ExistingXmlRoot.Attributes.Count; ii++)
            {
                for (int jj = 0; jj < NewXmlRoot.Attributes.Count; jj++)
                {
                    if (ExistingXmlRoot.Attributes[ii].Name == NewXmlRoot.Attributes[ii].Name) return true;
                }
            }
            return false;
        }

        bool RequestTranslation(string TranslationURI, DmlContainer InContext = null)
        {
            try
            {
                // Parse new URI so that we have a resolved header available.  Let's do this first, so that if there's an
                // error we see it before we attach the new translation.
                DomResolvedTranslation Resolved = DmlTranslationDocument.LoadTranslation(TranslationURI, this, ParsingOptions) as DomResolvedTranslation;
                if (Resolved == null) throw new Exception("Expected complete DOM resolution of new translation.");
                DomResolvedTranslation ExistingResolved = Document.ResolvedHeader as DomResolvedTranslation;
                if (ExistingResolved == null) throw new Exception("Expected complete DOM resolution of header.");
                if (IsXmlRootConflict(Resolved.XmlRoot, ExistingResolved.XmlRoot))
                    throw new Exception("Cannot add new translation '" + TranslationURI + "' because the XmlRoot element conflicts with the existing header.");

                // We try to integrate the changes without needing to reparse the entire header, but that isn't always possible.
                bool NeedReparse = false;

                if (InContext != null)
                {
                    // Add reference in header...
                    if (!(InContext.Association is DomAssociation)) throw new NotSupportedException("Cannot retrieve original translation definition for this context.");
                    DmlDefinition RDefinition = ((DomAssociation)InContext.Association).OriginalDefinition;
                    if (!(RDefinition is DmlContainerDefinition)) throw new NotSupportedException("Cannot retrieve original translation container definition for this context.");
                    DmlContainerDefinition Definition = (DmlContainerDefinition)RDefinition;
                    if (Definition.Document != Document) throw new NotSupportedException("Cannot add translation inclusion to external document.");
                    Definition.Children.Add(new DmlIncludeTranslation(Definition, TranslationURI));

                    // Apply reference in header to update available translations.
                    if (InContext.Association.LocalTranslation == null)
                        InContext.Association.LocalTranslation = new DmlTranslation(InContext.ActiveTranslation);
                    InContext.Association.LocalTranslation.Add(Resolved.Translation);
                    ExistingResolved.XmlRoot.Merge(Resolved.XmlRoot);
                    if (NeedReparseForPrimitiveSets(InContext.Document.ResolvedHeader.PrimitiveSets, Resolved.PrimitiveSets)) NeedReparse = true;     
                }
                else
                {
                    DmlIncludeTranslation NewInclude = new DmlIncludeTranslation(Document.Header, TranslationURI);
                    Document.Header.Children.Add(NewInclude);

                    Document.GlobalTranslation.Add(Resolved.Translation);
                    ExistingResolved.XmlRoot.Merge(Resolved.XmlRoot);
                    if (NeedReparseForPrimitiveSets(ExistingResolved.PrimitiveSets, Resolved.PrimitiveSets)) NeedReparse = true;
                }

                // We tried to make the changes just where necessary, but primitive sets proved to be messy.  Fortunately, primitive sets
                // don't effect the in-memory representation of the document, so we could almost just ignore this problem; however, to maintain a
                // consistent state for the primitive set information itself we reparse the entire header.  We discard most of the reparse since
                // the document is already referencing our existing resolved header, but we keep the new primitive set information.
                if (NeedReparse)
                {
                    ResolvedTranslation NewResolved = Document.Header.ToTranslation(this, ParsingOptions);
                    Document.ResolvedHeader.PrimitiveSets = NewResolved.PrimitiveSets;
                }

                return true;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); return false; }
        }

        void RefreshDmlHeader()
        {
            try
            {
                foreach (TreeNode PossibleNode in DocTree.Nodes)
                {
                    DmlNodeInfo NodeInfo = PossibleNode.Tag as DmlNodeInfo;
                    if (NodeInfo != null && NodeInfo.Node == Document.Header)
                    {
                        DocTree.Nodes.Remove(PossibleNode);
                        TreeNode NewTreeNode = DmlToTree(Document.Header, null);
                        DocTree.Nodes.Insert(0, NewTreeNode);
                        return;
                    }
                }
            }
            catch (Exception ex) { throw new Exception("While refreshing DML:Header: " + ex.Message, ex); }
        }

        #endregion

        #region "Middle-Pane (Attributes and Elements) UI"

        void InitMidPane()
        {
        }

        void BuildAttrList()
        {
            try
            {
                DmlPrimitiveInfo PrevSelected = AttrList.SelectedItem as DmlPrimitiveInfo;
                AttrList.Items.Clear();

                DmlContainerInfo SelectedContainerInfo = this.SelectedContainerInfo as DmlContainerInfo;
                if (SelectedContainerInfo != null)
                {
                    foreach (DmlPrimitiveInfo AttrInfo in SelectedContainerInfo.Attributes)
                    {
                        if (IsRecognizedDetail(AttrInfo.Primitive)) continue;
                        AttrList.Items.Add(AttrInfo);
                        if (AttrInfo == PrevSelected) AttrList.SelectedItem = AttrInfo;
                    }
                    AttrList.Enabled = true;
                }
                else
                {
                    AttrList.Enabled = false;
                }
            }
            catch (Exception ex) { throw new Exception("While building attribute list: " + ex.Message, ex); }
        }

        void BuildElementList()
        {
            try
            {
                DmlNodeInfo PrevSelected = ElementList.SelectedItem as DmlNodeInfo;                
                ElementList.Items.Clear();
                foreach (DmlNodeInfo NodeInfo in SelectedContainerInfo.Children)
                {
                    if (IsRecognizedDetail(NodeInfo.Node)) continue;

                    ElementList.Items.Add(NodeInfo);
                    if (NodeInfo == PrevSelected) ElementList.SelectedItem = NodeInfo;
                }
            }
            catch (Exception ex) { throw new Exception("While building element list: " + ex.Message, ex); }
        }

        private void AttrList_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                switch (e.KeyCode)
                {
                    case Keys.Insert:
                        {
                            if (SelectedContainer == null) return;
                            OnAddAttribute(null, null);
                            break;
                        }

                    case Keys.Delete:
                        {
                            if (AttrList.SelectedItem == null) return;
                            DmlPrimitiveInfo SelectedAttrInfo = AttrList.SelectedItem as DmlPrimitiveInfo;
                            if (SelectedAttrInfo == null) return;
                            DmlContainerInfo SelectedContainerInfo = SelectedAttrInfo.ContainerInfo as DmlContainerInfo;
                            if (SelectedContainerInfo == null) return;      // Must be a fragment...shouldn't really get here though.

                            SelectedContainerInfo.Attributes.Remove(SelectedAttrInfo);
                            SelectedContainerInfo.Container.Attributes.Remove(SelectedAttrInfo.Primitive);
                            AttrList.Items.Remove(SelectedAttrInfo);
                            break;
                        }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        bool RefreshOnly = false;
        private void AttrList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RefreshOnly) return;
            try
            {
                if (AttrList.SelectedIndex >= 0)
                {
                    SelectedPrimary = AttrList.SelectedItem as DmlNodeInfo;        // Setting SelectedPrimary invokes LoadPrimary().
                    ElementList.SelectedIndex = -1;
                }
            }
            catch (Exception ex) { throw new Exception("While processing attribute selection: " + ex.Message, ex); }
        }

        private void ElementList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RefreshOnly) return;
            try
            {
                if (ElementList.SelectedIndex >= 0)
                {
                    SelectedPrimary = ElementList.SelectedItem as DmlNodeInfo;        // Setting SelectedPrimary invokes LoadPrimary().
                    AttrList.SelectedIndex = -1;
                }
            }
            catch (Exception ex) { throw new Exception("While processing element selection: " + ex.Message, ex); }
        }

        #endregion

        #region "Rightmost Pane (Value or Composite) UI"

        PlaceholderPanel PlaceholderPanel = new PlaceholderPanel();
        PrimitivePanel PrimitivePanel = new PrimitivePanel();
        TablePanel TablePanel = new TablePanel();
        ImagePanel ImagePanel = new ImagePanel();

        void InitPrimaryPanels()
        {
            try
            {                
                Controls.Add(PrimitivePanel);

                PrimitivePanel.Left = AttrList.Right + 10;
                PrimitivePanel.Top = DocTree.Top;
                PrimitivePanel.Visible = false;
                PrimitivePanel.OnSummaryChanged += new PrimitivePanel.OnSummaryChangedHandler(OnPrimitiveSummaryChanged);
                PrimitivePanel.EnsurePrimitiveSet += new PrimitivePanel.EnsurePrimitiveSetHandler(OnEnsurePrimitiveSet);
                PrimitivePanel.ReplacePrimitive += new PrimitivePanel.ReplacePrimitiveHandler(ReplaceSelectedPrimitive);
                Width = Math.Max(PrimitivePanel.Right + 15, Width);

                Controls.Add(PlaceholderPanel);

                PlaceholderPanel.Left = PrimitivePanel.Left;;
                PlaceholderPanel.Top = PrimitivePanel.Top;
                PlaceholderPanel.Visible = false;

                Controls.Add(TablePanel);

                TablePanel.Left = PrimitivePanel.Left;
                TablePanel.Top = PrimitivePanel.Top;
                TablePanel.Visible = false;
                Width = Math.Max(TablePanel.Right + 15, Width);

                Controls.Add(ImagePanel);

                ImagePanel.Left = PrimitivePanel.Left;
                ImagePanel.Top = PrimitivePanel.Top;
                ImagePanel.Visible = false;
                Width = Math.Max(ImagePanel.Right + 15, Width);
            }
            catch (Exception ex) { throw new Exception("While initializing primary panels: " + ex.Message, ex); }
        }        

        void LoadPrimary()
        {
            try
            {
                if (SelectedPrimary is DmlPrimitiveInfo)
                {
                    PlaceholderPanel.Visible = false;
                    PrimitivePanel.SelectedPrimitiveInfo = (DmlPrimitiveInfo)SelectedPrimary;
                    PrimitivePanel.Visible = true;
                    TablePanel.Visible = false;
                    ImagePanel.Visible = false;
                }
                else if (SelectedPrimary is DmlTableInfo)
                {
                    PlaceholderPanel.Visible = false;
                    TablePanel.TableInfo = (DmlTableInfo)SelectedPrimary;
                    TablePanel.Visible = true;
                    PrimitivePanel.Visible = false;
                    ImagePanel.Visible = false;
                }
                else if (SelectedPrimary is DmlImageInfo)
                {
                    PlaceholderPanel.Visible = false;
                    ImagePanel.ImageInfo = (DmlImageInfo)SelectedPrimary;
                    ImagePanel.Visible = true;
                    TablePanel.Visible = false;
                    PrimitivePanel.Visible = false;
                }
                else
                {
                    PlaceholderPanel.Visible = true;
                    TablePanel.Visible = false;
                    PrimitivePanel.Visible = false;
                    ImagePanel.Visible = false;
                }
            }
            catch (Exception ex) { 
#               if DEBUG
                throw new Exception(ex.Message + "\nError while loading primary selection.\nDetail: \n\n" + ex.ToString(), ex); 
#               else
                throw new Exception(ex.Message + "\nError while loading primary selection.", ex); 
#               endif
            }
        }

        void OnPrimitiveSummaryChanged()
        {
            try
            {
                RefreshOnly = true;
                DmlPrimitiveInfo SelectedPrimitive = SelectedPrimary as DmlPrimitiveInfo;
                //if (SelectedPrimitive.Primitive.IsAttribute) AttrList.RefreshItem(AttrList.SelectedIndex);
                //else ElementList.RefreshItem(ElementList.SelectedIndex);
                if (SelectedPrimitive.Primitive.IsAttribute)
                {
                    int index = AttrList.SelectedIndex;
                    AttrList.Items.RemoveAt(index);
                    AttrList.Items.Insert(index, SelectedPrimitive);
                    AttrList.SelectedIndex = index;
                }
                else
                {
                    int index = ElementList.SelectedIndex;
                    ElementList.Items.RemoveAt(index);
                    ElementList.Items.Insert(index, SelectedPrimitive);
                    ElementList.SelectedIndex = index;
                }
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\nError while processing summary change.", ex); }
            finally { RefreshOnly = false; }
        }

        void ReplaceSelectedPrimitive(DmlPrimitive NewPrim)
        {
            try
            {
                DmlPrimitive SelectedPrimitive = (SelectedPrimary as DmlPrimitiveInfo).Primitive;
                DmlContainer CurrContainer = SelectedPrimitive.Container as DmlContainer;
                if (SelectedPrimitive.IsAttribute)
                {
                    int index = CurrContainer.Attributes.IndexOf(SelectedPrimitive);
                    CurrContainer.Attributes.RemoveAt(index);
                    CurrContainer.Attributes.Insert(index, NewPrim);
                }
                else
                {
                    int index = CurrContainer.Children.IndexOf(SelectedPrimitive);
                    CurrContainer.Children.RemoveAt(index);
                    CurrContainer.Children.Insert(index, NewPrim);
                }
                SelectedPrimary.Node = NewPrim;
            }
            catch (Exception ex) { throw new Exception(ex.Message + "\nError while replacing/updating current primitive.", ex); }
        }

        #endregion

        public bool HideRecognizedDetails
        {
            get { return hideRecognizedDetailsToolStripMenuItem.Checked; }
        }

        

        private void hideRecognizedDetailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            hideRecognizedDetailsToolStripMenuItem.Checked = !hideRecognizedDetailsToolStripMenuItem.Checked;
            //DoRecognizedDetails();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox ab = new AboutBox();
            ab.ShowDialog();
        }

        private void translationToCClassToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (Document.Name != "DML:Translation")
                {
                    MessageBox.Show("Please load a DML:Translation document before converting to code.");
                    return;
                }

                TextPromptForm tpf = new TextPromptForm();
                tpf.Text = "Please provide information for conversion";
                tpf.PromptLabel.Text = "Enter name for class:";
                tpf.UserText.Text = "MyTranslation";
                if (tpf.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                string Output = DmlTranslationToCSharpClass.Convert(Document, this, tpf.UserText.Text);

                string TempFileName = System.IO.Path.GetTempFileName();
                using (StreamWriter SW = File.CreateText(TempFileName))
                    SW.Write(Output);

                Process.Start("Notepad.exe", TempFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void translationToCClassToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (Document.Name != "DML:Translation")
                {
                    MessageBox.Show("Please load a DML:Translation document before converting to code.");
                    return;
                }

                TextPromptForm tpf = new TextPromptForm();
                tpf.Text = "Please provide information for conversion";
                tpf.PromptLabel.Text = "Enter name for class:";
                tpf.UserText.Text = "MyTranslation";
                if (tpf.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                string Output = DmlTranslationToCPPClass.Convert(Document, this, tpf.UserText.Text);

                string TempFileName = System.IO.Path.GetTempFileName();
                using (StreamWriter SW = File.CreateText(TempFileName))
                    SW.Write(Output);

                Process.Start("Notepad.exe", TempFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void defaultDMLProgramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Make Dml Editor your default DML file association?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            string ExecutablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            Registry.SetValue(AppKey, "DoNotAssociate", (uint)0);
            Utility.SetAssociation(".dml", HandlerName, ExecutablePath, FileTypeDescription);
        }

        private void defaultDVidProgramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Make Dml Editor your default dVid file association?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            string ExecutablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;            
            Utility.SetAssociation(".dVid", VidHandlerName, ExecutablePath, VidFileTypeDescription);
        }

        private void showDiagnosticInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            if (SelectedContainer != null)
            {
                sb.AppendLine("Selected Container:");
                sb.AppendLine();
                sb.AppendLine("\tName:\t\t\t" + SelectedContainer.Name);
                if (SelectedContainer.DmlID == DmlTranslation.DML3.InlineIdentification.DMLID)
                    sb.AppendLine("\tDmlID:\t\t\tInline Identification");
                else
                {
                    string strDmlId = SelectedContainer.DmlID.ToString();
                    DmlFragment Parent = SelectedContainer.Container;
                    while (Parent != null)
                    {
                        if (Parent is DmlContainer)
                            strDmlId = Parent.DmlID.ToString() + ":" + strDmlId;                        
                        Parent = Parent.Container;
                    }
                    sb.AppendLine("\tDmlID:\t\t\t" + strDmlId);
                }
                if (SelectedContainer.StartPosition != long.MaxValue)
                {
                    sb.AppendLine();
                    sb.AppendLine("\tStart position in stream:\t" + SelectedContainer.StartPosition);
                }
                MessageBox.Show(sb.ToString(), "Node Information");
            }
        }

        private void clearResourceCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearResourceRedirectCache();
        }        
    }
   
}
