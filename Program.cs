using System;
using System.Reflection;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace UserDataBackup {
    static class Program {

        public readonly static string UserFolderRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public readonly static string OneDriveRoot = $@"{UserFolderRoot}\OneDrive - militaryhealth";
        public readonly static string BackupRoot = $@"{OneDriveRoot}\Backup";

        static Program() {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
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
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            if (args.Name is null)
                throw new NullReferenceException("Item name is null and could not be resolved.");
            if (!executingAssembly.GetManifestResourceNames().Contains("Many_Objects_Display.Resources." + new AssemblyName(args.Name).Name.Replace(".resources", ".dll")))
                throw new ArgumentException("Resource name does not exist.");
            Stream resourceStream = executingAssembly.GetManifestResourceStream("Many_Objects_Display.Resources." + new AssemblyName(args.Name).Name.Replace(".resources", ".dll")) ?? throw new NullReferenceException("Resource stream is null.");
            if (resourceStream.Length > 104857600)
                throw new ArgumentException("Exceedingly long resource - greater than 100MB. Aborting ...");
            byte[] block = new byte[resourceStream.Length];
            resourceStream.Read(block, 0, block.Length);
            Assembly resourceAssembly = Assembly.Load(block) ?? throw new NullReferenceException("Assembly is a null value.");
            return resourceAssembly;
        }
    }
}
