using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using Microsoft.Win32;
using WileyBlack.Dml;
using WileyBlack.Dml.Dom;
using System.Runtime.InteropServices;

namespace Dml_Editor
{    
    public static class Utility
    {
        /// <summary>
        /// Configures the Windows registry to associate a file with an executable.  This association operates on a per-user basis
        /// in order to avoid the need for administrative priviledges.  To use a per-system configuration, use CurrentMachine instead
        /// of CurrentUser.
        /// </summary>
        /// <param name="Extension">The file extension to associate, with the dot.  For example, ".dml".</param>
        /// <param name="KeyName">The handler name.  This is basically arbitrary, an example would be "Excel.CSV".</param>
        /// <param name="OpenWith">Path to the executable to handle the file type.  The path does not require quotes.</param>
        /// <param name="FileDescription">A description to be displayed to the user for this file type.  An example
        /// is "Microsoft Excel Comma Separated Values File".</param>
        public static void SetAssociation(string Extension, string KeyName, string OpenWith, string FileDescription)
        {
            RegistryKey ClassesKey;
            RegistryKey BaseKey;
            RegistryKey OpenMethod;
            RegistryKey Shell;            

            ClassesKey = Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey("Classes",true);
            BaseKey = ClassesKey.CreateSubKey(Extension);
            BaseKey.SetValue("", KeyName);

            OpenMethod = ClassesKey.CreateSubKey(KeyName);
            OpenMethod.SetValue("", FileDescription);
            OpenMethod.CreateSubKey("DefaultIcon").SetValue("", "\"" + OpenWith + "\",0");
            Shell = OpenMethod.CreateSubKey("Shell");
            Shell.CreateSubKey("edit").CreateSubKey("command").SetValue("", "\"" + OpenWith + "\"" + " \"%1\"");
            Shell.CreateSubKey("open").CreateSubKey("command").SetValue("", "\"" + OpenWith + "\"" + " \"%1\"");
            BaseKey.Close();
            OpenMethod.Close();
            Shell.Close();

            /**
            RegistryKey CurrentUser;
            
            CurrentUser = Registry.CurrentUser.CreateSubKey(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.ucs");
            CurrentUser = CurrentUser.OpenSubKey("UserChoice", RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl);
            CurrentUser.SetValue("Progid", KeyName, RegistryValueKind.String);
            CurrentUser.Close();

            // Delete the key instead of trying to change it
            CurrentUser = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\.ucs", true);
            CurrentUser.DeleteSubKey("UserChoice", false);
            CurrentUser.Close();
            */

            // Tell explorer the file association has been changed
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);             
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        /// <summary>
        /// Tests whether a specified executable is the current handler for a file extension.
        /// </summary>
        /// <param name="Extension">The file extension to associate, with the dot.  For example, ".dml".</param>        
        /// <param name="OpenWith">Path to the executable to handle the file type.  The path does not require quotes.</param>        
        public static bool IsAssociated(string Extension, string OpenWith)
        {
            if (IsAssociated(Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("Classes"), Extension, OpenWith)) return true;
            if (IsAssociated(Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("Classes"), Extension, OpenWith)) return true;
            if (IsAssociated(Registry.ClassesRoot, Extension, OpenWith)) return true;
            return false;
        }

        private static bool IsAssociated(RegistryKey ClassesKey, string Extension, string OpenWith)
        {
            try
            {
                RegistryKey BaseKey = ClassesKey.OpenSubKey(Extension);
                string KeyName = (string)BaseKey.GetValue("", "");
                if (string.IsNullOrEmpty(KeyName)) return false;
                BaseKey.Close();

                RegistryKey OpenMethod = ClassesKey.OpenSubKey(KeyName);
                RegistryKey Shell = OpenMethod.OpenSubKey("Shell");
                RegistryKey Open = Shell.OpenSubKey("open");
                RegistryKey Command = Open.OpenSubKey("command");
                string OpenCommand = (string)Command.GetValue("", "");
                Command.Close(); Open.Close(); Shell.Close(); OpenMethod.Close();
                OpenWith = OpenWith.Trim().ToLowerInvariant();
                return (OpenCommand.ToLowerInvariant().Contains(OpenWith));
            }
            catch (Exception) { return false; }
        }
    }
}
