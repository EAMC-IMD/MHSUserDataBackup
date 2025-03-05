using System;
using System.Reflection;
using System.Windows.Forms;
using UserDataBackup.Forms;

#nullable enable
namespace UserDataBackup {
    static class Program {

        public static readonly string UserFolderRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public static readonly string OneDriveRoot = Environment.GetEnvironmentVariable("OneDriveCommercial");
        public static readonly string BackupRoot = $@"{OneDriveRoot}\Backup";

        static Program() {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            _ = Assembly.Load(Properties.Resources.SapphTools_BookmarkManager_Chromium);
            _ = Assembly.Load(Properties.Resources.Newtonsoft_Json);
            _ = Assembly.Load(Properties.Resources.OneDrive);
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }

        static Assembly? CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            string dllName = args.Name.Contains(",") ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll","");
            dllName = dllName.Replace(".", "_");
            if (dllName.EndsWith("_resources"))
                return null;
            System.Resources.ResourceManager rm = new System.Resources.ResourceManager("UserDataBackup.Properties.Resources", Assembly.GetExecutingAssembly());
            byte[] bytes = (byte[])rm.GetObject(dllName);
            if (bytes == null) 
                return null;
            return Assembly.Load(bytes);
            //switch (args.Name) {
            //    case "OneDrive.resources, Version=1.0.0.0, Culture=en-US, PublicKeyToken=null":
            //    case "OneDrive, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null":
            //    case "OneDrive.resources, Version=1.0.0.0, Culture=en, PublicKeyToken=null":
            //        return Assembly.Load(Properties.Resources.OneDrive);
            //    case "SapphTools_BookmarkManager_Chromium.resources, Version=1.0.0.0, Culture=en-US, PublicKeyToken=null":
            //    case "SapphTools_BookmarkManager_Chromium, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null":
            //        return Assembly.Load(Properties.Resources.SapphTools_BookmarkManager_Chromium);
            //    case "Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed":
            //        return Assembly.Load(Properties.Resources.Newtonsoft_Json);
            //    default:
            //        throw new NullReferenceException("Unknown assembly.");
            //}
        }
    }
}
