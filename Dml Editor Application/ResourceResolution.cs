using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WileyBlack.Dml;
using WileyBlack.Dml.Dom;
using WileyBlack.Dml.EC;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Dml_Editor.Conversion;
using Microsoft.Win32;

namespace Dml_Editor
{
    public partial class MainForm
    {
        private string TryCachedResourceRedirect(string Uri, out bool IsXml)
        {
            RegistryKey Cache = Registry.CurrentUser.OpenSubKey(RelAppKey + @"\Cached_Resource_Redirect");
            if (Cache == null) { IsXml = false; return null; }

            foreach (string ValueName in Cache.GetValueNames())
            {
                if (string.Equals(ValueName, Uri, StringComparison.InvariantCultureIgnoreCase))
                {
                    IsXml = (System.IO.Path.GetExtension(ValueName).ToLower() == ".xml") || ValueName.ToLower().Contains(".xml");
                    return Cache.GetValue(ValueName) as string;
                }
            }
            IsXml = false;
            return null;
        }

        private void CacheResourceRedirect(string SrcUri, string DstUri, bool IsXml)
        {
            RegistryKey Cache = Registry.CurrentUser.CreateSubKey(RelAppKey + @"\Cached_Resource_Redirect", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (Cache == null) return;

            Cache.SetValue(SrcUri, DstUri);
        }

        public void ClearResourceRedirectCache()
        {
            Registry.CurrentUser.DeleteSubKey(RelAppKey + @"\Cached_Resource_Redirect", false);
        }

        private IDisposable TryResolve(string Uri, out bool IsXml)
        {
            Uri uri = new Uri(Uri);
            if (uri.Scheme != "urn")
            {
                try
                {
                    WebClient wc = new WebClient();
                    Stream PrimaryStream = wc.OpenRead(Uri);

                    IsXml = uri.PathAndQuery.ToLower().Contains(".xml");
                    return PrimaryStream;
                }
                catch (Exception)
                {
                    IsXml = false; return null;
                }
            }
            else
            {
                if (Uri.ToLower() == DmlTranslation.DML3.urn.ToLower()) { IsXml = false; return new ResolvedTranslation(DmlTranslation.DML3, null); }
                if (Uri.ToLower() == TSL2Translation.urn.ToLower()) { IsXml = false; return new ResolvedTranslation(DmlTranslation.TSL2, null); }
                if (Uri.ToLower() == EC2Translation.urn.ToLower()) { IsXml = false; return new ResolvedTranslation(DmlTranslation.EC2, DmlTranslation.EC2.RequiredPrimitiveSets); }
            }

            IsXml = false;
            return null;
        }

        public IDisposable Resolve(string Uri, out bool IsXml)
        {
            for (; ; )
            {
                Uri uri = new Uri(Uri);

                IDisposable Attempt = TryResolve(Uri, out IsXml);
                if (Attempt != null) return Attempt;

                if (uri.Scheme != "urn")
                {
                    switch (
                        MessageBox.Show(
                            "Unable to retrieve required resource '" + Uri + "'.  Would you like to provide "
                            + "this resource (file) from another location?  (Select NO to retry the original URI.)",
                            "Error locating resource",
                            MessageBoxButtons.YesNoCancel))
                    {
                        case DialogResult.No: continue;
                        case DialogResult.Yes: break;
                        default:
                        case DialogResult.Cancel: throw new Exception("Unable to retrieve resource '" + Uri + "'.");
                    }
                }
                else
                {
                    string Redirect = TryCachedResourceRedirect(Uri, out IsXml);
                    if (!string.IsNullOrEmpty(Redirect))
                    {
                        Attempt = TryResolve(Redirect, out IsXml);
                        if (Attempt != null) return Attempt;
                    }
                }

                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Dml translation files (*.dml,*.xml)|*.dml;*.xml";
                ofd.Title = "Please locate the DML translation document '" + Uri + "'...";
                if (ofd.ShowDialog() != DialogResult.OK) throw new Exception("Unable to retrieve resource '" + Uri + "'.");                
                Attempt = ResourceFromFile(ofd.FileName, out IsXml);
                if (Attempt != null)
                {
                    CacheResourceRedirect(Uri, ofd.FileName, IsXml);
                    return Attempt;
                }
            }
        }

        Stream ResourceFromFile(string Path, out bool IsXml)
        {
            IsXml = (System.IO.Path.GetExtension(Path).ToLower() == ".xml");
            return new FileStream(Path, FileMode.Open);
        }
    }
}
