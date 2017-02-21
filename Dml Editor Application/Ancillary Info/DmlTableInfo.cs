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

namespace Dml_Editor
{
    public class DmlColumnInfo : DmlContainerInfo
    {
        public string Name
        {
            get
            {
                if (Container.Attributes["Name"] as DmlString == null) return null;
                return ((DmlString)Container.Attributes["Name"]).Value as string;
            }
            set
            {
                Container.SetAttribute("Name", value);
            }
        }

        public DmlArray Data
        {
            get
            {
                if (Container.Children["Data"] as DmlArray == null) return null;
                return (DmlArray)Container.Children["Data"];
            }
            set
            {
                for (int ii = 0; ii < Container.Children.Count; )
                {
                    if (Container.Children[ii].Name == "Data") Container.Children.RemoveAt(ii);
                    else ii++;
                }
                value.Name = "Data";
                Container.Children.Add(value);
            }
        }

        public DmlColumnInfo(DmlContainer dml, DmlTableInfo ContainerInfo)        
            : base(dml, ContainerInfo)
        {
            if (dml.Name != "Column")
                throw new FormatException("Cannot parse table column from dml - expected 'Column'.");
        }
    }

    public class DmlTableInfo : DmlContainerInfo
    {
        public bool AddCompression = false;

        private DmlColumnInfo[] m_Columns;
        public DmlColumnInfo[] Columns
        {
            get
            {
                return m_Columns;
            }
            set
            {
                m_Columns = value;

                RemoveDmlColumns(Container);

                if (!IsCompressed && AddCompression)
                {
                    DmlCompressed dc = new DmlCompressed(Container);
                    foreach (DmlColumnInfo dci in value) dc.DecompressedFragment.Children.Add(dci.Container);
                    Container.Children.Add(dc);
                }
                else
                {
                    foreach (DmlColumnInfo dci in value) Container.Children.Add(dci.Container);                    
                }
            }
        }

        private void RemoveDmlColumns(DmlFragment From)
        {
            for (int ii = 0; ii < From.Children.Count; )
            {
                if (From.Children[ii] is DmlCompressed)
                {
                    DmlCompressed CContent = (DmlCompressed)From.Children[ii];
                    RemoveDmlColumns(CContent.DecompressedFragment);
                    if (CContent.DecompressedFragment.Children.Count == 0)
                        From.Children.RemoveAt(ii);
                    else
                        ii++;
                }
                else if (From.Children[ii].Name == "Column" && From.Children[ii] is DmlContainer)
                    From.Children.RemoveAt(ii);
                else
                    ii++;
            }
        }

#       if false
        public string[] ColumnNames
        {
            get
            {
                if (Container.Attributes["Column-Names"] as DmlStringArray == null) return new string[0];
                return (string[])((DmlStringArray)Container.Attributes["Column-Names"]).Value;
            }
            set
            {
                Container.SetAttribute("Column-Names", value);
            }
        }

        public List<DmlArray> Columns 
        {
            get
            {
                List<DmlArray> ret = new List<DmlArray>();

                for (int iColumn = 0; iColumn < Container.Children.Count; iColumn++)
                {
                    if (!(Container.Children[iColumn] is DmlArray)) continue;
                    DmlArray Col = (DmlArray)Container.Children[iColumn];
                    if (Col.Name != "Column") continue;
                    ret.Add(Col);
                }

                return ret;
            }

            set
            {
                for (int iColumn = 0; iColumn < Container.Children.Count; )
                {
                    if (Container.Children[iColumn] is DmlArray
                     && ((DmlArray)Container.Children[iColumn]).Name == "Column")
                    {
                        Container.Children.RemoveAt(iColumn);
                    }
                    else iColumn++;
                }

                for (int iColumn = 0; iColumn < value.Count; iColumn++)
                {
                    if (value[iColumn].Name != "Column")
                        throw new FormatException("Expected only Column named items in Columns set.");
                    Container.Children.Add(value[iColumn]);
                }
            }
        }
#       endif

        public DmlTableInfo(DmlContainer Dml, DmlFragmentInfo ContainerInfo)
            : base(Dml, ContainerInfo)
        {
            if (Dml.Name != "Table")
                throw new FormatException("Cannot parse table from Dml.");            
            
            m_Columns = ReadColumns(Container).ToArray();
        }

        private List<DmlColumnInfo> ReadColumns(DmlFragment Dml)
        {
            List<DmlColumnInfo> set = new List<DmlColumnInfo>();
            foreach (DmlNode node in Dml.Children)
            {
                if (node is DmlCompressed)
                {
                    set.AddRange(ReadColumns(((DmlCompressed)node).DecompressedFragment));
                    AddCompression = true;
                    continue;
                }
                if (node is DmlContainer && node.Name == "Column")
                {
                    DmlColumnInfo dci = new DmlColumnInfo((DmlContainer)node, this);
                    set.Add(dci);
                }
            }
            return set;
        }

        public override bool IsRecognizedDetail(DmlNode Detail)
        {
            if (Detail.Name == "Column") return true;
            if (Detail.Name == "Data") return true;
            if (Detail is DmlCompressed) return true;
            if (!(Detail is DmlPrimitive)) return false;
            return false;
        }
    }
}
