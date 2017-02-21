using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using WileyBlack.Dml;
using WileyBlack.Dml.Dom;
using System.Diagnostics;

namespace Dml_Editor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

#           if false
            DmlWriter writer = DmlWriter.Create("C:\\Temporary\\tmp.dml");
            writer.AddPrimitiveSet("common", "le");
            writer.AddPrimitiveSet("arrays", "le");
            writer.WriteStartContainer("DML:Header");
            writer.Write("DocType", "");
            writer.WriteEndAttributes();
            writer.WriteStartContainer("DML:Include");
            writer.Write("DML:Primitives", "common");
            writer.Write("DML:Codec", "le");
            writer.WriteEndContainer();
            writer.WriteStartContainer("DML:Include");
            writer.Write("DML:Primitives", "arrays");
            writer.Write("DML:Codec", "le");
            writer.WriteEndContainer();
            writer.WriteEndContainer();
            writer.WriteStartContainer("Example");
            try
            {
                DmlContainer Table = new DmlContainer();
                Table.Name = "Table";
                Table.AddAttribute("Column-Names", new string[] { "First", "Second", "Third" });
                Table.AddElement("Column", new double[5]);
                Table.AddElement("Column", new double[5]);
                Table.WriteTo(writer);
                writer.WriteEndContainer();
                writer.Close();
                writer.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write("Exception:\n" + ex.ToString() + "\n\n");
                return;
            }
#           endif

                MainForm mf = new MainForm();

                if (args.Length > 0)
                {
                    try
                    {
                        mf.OpenFile(args[0]);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to open file '" + args[0] + "': " + ex.Message);
                        return;
                    }
                }

                Application.Run(mf);
            }
            catch (Exception exc)
            {
                MessageBox.Show("Fatal Error: " + exc.Message + "\n\nDetail:\n" + exc.ToString());
            }
        }
    }
}
