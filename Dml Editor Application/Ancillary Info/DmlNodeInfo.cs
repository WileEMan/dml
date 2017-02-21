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
    public class DmlNodeInfo
    {
        public DmlNode Node;
        public DmlFragmentInfo ContainerInfo;        

        public bool IsCompressed
        {
            get
            {
                DmlFragmentInfo Check = ContainerInfo;
                while (Check != null)
                {
                    if (Check is DmlCompressedInfo) return true;
                    Check = Check.ContainerInfo;
                }
                return false;
            }
        }

        public DmlNodeInfo(DmlNode Node, DmlFragmentInfo ContainerInfo)
        {
            this.Node = Node;
            this.ContainerInfo = ContainerInfo;            
        }

        protected DmlNodeInfo()
        {
        }
    }

    public class DmlPrimitiveInfo : DmlNodeInfo
    {
        public DmlPrimitive Primitive
        {
            get { return Node as DmlPrimitive; }
            set { Node = value; }
        }        

        public DmlPrimitiveInfo(DmlPrimitive Primitive, DmlFragmentInfo ContainerInfo)
            : base(Primitive, ContainerInfo)
        {
        }

        public override string ToString()
        {
            if (Primitive.IsAttribute)
            {
                switch (Primitive.PrimitiveType)
                {
                    case PrimitiveTypes.Array:
                        return Primitive.Name + " = (Array)";
                    case PrimitiveTypes.Boolean:
                        return Primitive.Name + " = " + ((bool)((Primitive as DmlBool).Value) ? "true" : "false");
                    case PrimitiveTypes.CompressedDML:
                        return Primitive.Name + " - compressed DML";                    
                    case PrimitiveTypes.DateTime:
                        return Primitive.Name + " = " + ((DateTime)(Primitive as DmlDateTime).Value).ToUniversalTime().ToString() + " UTC";
                    //case PrimitiveTypes.Decimal:
                    case PrimitiveTypes.Double:
                        return Primitive.Name + " = " + ((double)(Primitive as DmlDouble).Value).ToString("F06");
                    case PrimitiveTypes.EncryptedDML:
                        return Primitive.Name + " - encrypted DML";
                    case PrimitiveTypes.Extension:
                        return Primitive.Name + " - extended primitive";
                    case PrimitiveTypes.Int:
                        return Primitive.Name + " = " + ((long)(Primitive as DmlInt).Value).ToString();
                    case PrimitiveTypes.Matrix:
                        return Primitive.Name + " = (Matrix)";
                    case PrimitiveTypes.Single:
                        return Primitive.Name + " = " + ((float)(Primitive as DmlSingle).Value).ToString();
                    case PrimitiveTypes.String:
                        return Primitive.Name + " = \"" + ((string)(Primitive as DmlString).Value) + "\"";
                    case PrimitiveTypes.UInt:
                        return Primitive.Name + " = " + ((ulong)(Primitive as DmlUInt).Value).ToString();
                    default:
                    case PrimitiveTypes.Unknown:
                        return Primitive.Name + " - unrecognized primitive";
                }
            }
            else
            {
                string OpenTag = "<" + Primitive.Name + ">";
                string CloseTag = "</" + Primitive.Name + ">";

                switch (Primitive.PrimitiveType)
                {
                    case PrimitiveTypes.Array:
                        return OpenTag + "[Array]" + CloseTag;
                    case PrimitiveTypes.Boolean:
                        return OpenTag + ((bool)(Primitive as DmlBool).Value ? "true" : "false") + CloseTag;
                    case PrimitiveTypes.CompressedDML:
                        return OpenTag + "[Compressed DML]" + CloseTag;                    
                    case PrimitiveTypes.DateTime:
                        return OpenTag + ((DateTime)(Primitive as DmlDateTime).Value).ToUniversalTime().ToString() + " UTC" + CloseTag;
                    //case PrimitiveTypes.Decimal:
                    case PrimitiveTypes.Double:
                        return OpenTag + ((double)(Primitive as DmlDouble).Value).ToString() + CloseTag;
                    case PrimitiveTypes.EncryptedDML:
                        return OpenTag + "[Encrypted DML]" + CloseTag;
                    case PrimitiveTypes.Extension:
                        return OpenTag + "[Extended Primitive]" + CloseTag;
                    case PrimitiveTypes.Int:
                        return OpenTag + ((long)(Primitive as DmlInt).Value).ToString() + CloseTag;
                    case PrimitiveTypes.Matrix:
                        return OpenTag + "[Matrix]" + CloseTag;
                    case PrimitiveTypes.Single:
                        return OpenTag + ((float)(Primitive as DmlSingle).Value).ToString() + CloseTag;
                    case PrimitiveTypes.String:
                        return OpenTag + ((string)(Primitive as DmlString).Value).ToString() + CloseTag;
                    case PrimitiveTypes.UInt:
                        return OpenTag + ((ulong)(Primitive as DmlUInt).Value).ToString() + CloseTag;
                    default:
                    case PrimitiveTypes.Unknown:
                        return OpenTag + "[Unrecognized Primitive]" + CloseTag;
                }
            }
        }
    }

    public class DmlFragmentInfo : DmlNodeInfo
    {
        public DmlFragment Fragment
        {
            get { return Node as DmlFragment; }
            set { Node = value; }
        }

        public List<DmlNodeInfo> Children = new List<DmlNodeInfo>();

        public DmlFragmentInfo(DmlFragment ThisDml, DmlFragmentInfo ContainerInfo)
            : base(ThisDml, ContainerInfo)
        {
        }

        protected DmlFragmentInfo()
        {
        }

        public virtual bool IsRecognizedDetail(DmlNode Node)
        {
            return false;
        }

        public override string ToString()
        {
            return "[Fragment]...[/Fragment]";
        }
    }

    public class DmlContainerInfo : DmlFragmentInfo
    {
        /// <summary>
        /// It gets confusing here.  'Container' refers to "this" container, at the current
        /// level, represented as a DmlContainer object.  'ContainerInfo', inherited, refers
        /// to the DmlContainerInfo parent object of this DmlContainerInfo object.
        /// </summary>
        public DmlContainer Container
        {
            get { return Node as DmlContainer; }
            set { Node = value; }
        }

        public List<DmlPrimitiveInfo> Attributes = new List<DmlPrimitiveInfo>();        

        public DmlContainerInfo(DmlContainer Container, DmlFragmentInfo ContainerInfo)
            : base(Container, ContainerInfo)
        {            
        }

        protected DmlContainerInfo()
        {
        }

        /**
        public DmlContainerInfo()
        {
        }
         */        

        public override string ToString()
        {
            if (Container.Loaded != DmlFragment.LoadState.Full)
                return "<" + Container.Name + ">...</" + Container.Name + ">";

            if (Container.Children.Count > 0)
                return "<" + Container.Name + ">...</" + Container.Name + ">";
            else
                return "<" + Container.Name + "/>";
        }
    }

    public class DmlCompressedInfo : DmlFragmentInfo
    {
        public DmlCompressed Compressed
        {
            get { return Node as DmlCompressed; }
            set { Node = value; }
        }

        public DmlCompressedInfo(DmlCompressed CompressedNode, DmlFragmentInfo ContainerInfo)
            : base(CompressedNode.DecompressedFragment, ContainerInfo)
        {
        }

        public override bool IsRecognizedDetail(DmlNode Node)
        {
            return false;
        }

        public override string ToString()
        {
            return "<Compressed>...</Compressed>";
        }
    }
}
