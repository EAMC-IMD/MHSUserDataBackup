using System;
using System.Reflection;
using System.Windows.Forms;


namespace UserDataBackup {
    static class Program {

        public static readonly string UserFolderRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public static readonly string OneDriveRoot = $@"{UserFolderRoot}\OneDrive - militaryhealth";
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

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            switch (args.Name) {
                case "OneDrive.resources, Version=1.0.0.0, Culture=en-US, PublicKeyToken=null":
                case "OneDrive, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null":
                case "OneDrive.resources, Version=1.0.0.0, Culture=en, PublicKeyToken=null":
                    return Assembly.Load(Properties.Resources.OneDrive);
                case "SapphTools_BookmarkManager_Chromium.resources, Version=1.0.0.0, Culture=en-US, PublicKeyToken=null":
                case "SapphTools_BookmarkManager_Chromium, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null":
                    return Assembly.Load(Properties.Resources.SapphTools_BookmarkManager_Chromium);
                case "Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed":
                    return Assembly.Load(Properties.Resources.Newtonsoft_Json);
                //case "UserDataManagement.resources, Version=2.1.2.0, Culture=en-US, PublicKeyToken=null":
                //    return Assembly.Load(Properties);
                default:
                    throw new NullReferenceException("Unknown assembly.");
            }
        }
    }
}
