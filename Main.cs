using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OneDrive;
using OneDrive.OdSyncService;

namespace UserDataBackup {
    public partial class Main : Form {
        private ProfileData data;
        private OneDriveStatus odStatus;
        private StatusDetail status;
        private System.Timers.Timer timer;
        public Main() {
            InitializeComponent();
        }
        private void Main_Load(object sender, EventArgs e) {
            if (!Directory.Exists(Program.OneDriveRoot)) {
                Backup.Enabled = false;
                Restore.Enabled = false;
                statusLabel.Text = "This version of User Data Management is intended for OneDrive only.";
                statusImage.Image = Properties.Resources.OneDriveError;
                statusImage.Refresh();
                if (Process.GetProcessesByName("OneDrive").Count() == 0)
                    Process.Start(@"C:\Program Files\Microsoft OneDrive\OneDrive.exe");
            } else {
                data = new ProfileData(Program.OneDriveRoot, Program.BackupRoot);
                odStatus = new OneDriveStatus();
                status = odStatus.GetStatus().Where(s => s.LocalPath == Program.OneDriveRoot).First();
                if (status.StatusString == "No internet connection") {
                    statusImage.Image = Properties.Resources.offline_icon_19;
                } else {
                    statusImage.Image = Properties.Resources.onedrive;
                    statusImage.Image = Properties.Resources.SyncComplete2;
                }
                statusImage.Refresh();
            }
        }
        private void RunBackup() {
            if (Process.GetProcessesByName("OneDrive").Count() == 0)
                Process.Start(@"C:\Program Files\Microsoft OneDrive\OneDrive.exe");
            odStatus = new OneDriveStatus();
            status = odStatus.GetStatus().Where(s => s.LocalPath == Program.OneDriveRoot).First();
            string message;
            if (status.StatusString == "No internet connection") {
                message = "This program backs data up to OneDrive.  However, you do not currently have an active network connection. " +
                    "You can still run the backup, and it will be queued for update to OneDrive next time you connect. You should NOT treat this " +
                    "backup as complete until you are connected back to the MHS network, and your OneDrive icon shows 'Up to Date'.";
                MessageBox.Show(message, "Offline Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            message = "In order to complete the backup, Outlook, Sticky Notes, AsUType and all browsers must be closed. Please ensure you have no unsaved work before continuing.";
            if (MessageBox.Show(message, "Process Exit Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                return;
            statusLabel.Text = "Backup in progress.";
            data.Backup();
            if (odStatus.GetStatus().Where(s => s.LocalPath == Program.OneDriveRoot).First().StatusString == "No internet connection")
                return;
            statusLabel.Text = "Sync in progress.";
            message = "Your bookmarks, sticky notes, Outlook signatures, and AsUType shortcuts (if extant) have been backed up to the Backup folder in your OneDrive.  " +
                "However, it is critical you do not log out, remove your CAC, reboot, or interrupt your network connection until sync is complete.";
            MessageBox.Show(message, "Sync In Progress", MessageBoxButtons.OK, MessageBoxIcon.Information);
            status = odStatus.GetStatus().Where(s => s.LocalPath == Program.OneDriveRoot).First();
            statusImage.Image = Properties.Resources.SyncInPrgress;
            statusImage.Refresh();
            if (status.StatusString != "Up to date") {
                this.Cursor = Cursors.WaitCursor;
                timer = new System.Timers.Timer(1000);
                timer.Elapsed += CheckForSync;
                timer.AutoReset = true;
                timer.Enabled = true;
                timer.SynchronizingObject = this;
                timer.Start();
            }

        } 
        private void RunRestore() {
            Cursor = Cursors.WaitCursor;
            data.Restore();
            Cursor = Cursors.Default;
            string message = "Restore operations complete! Here are the results of each item:" + Environment.NewLine;
            bool nonewprofile = false;
            foreach (var result in data.Targets) {
                message += $"\t{result.Key} : {result.Value.RestoreResult}" + Environment.NewLine;
                nonewprofile ^= result.Value.RestoreResult == ProfileData.RestoreResult.NoNewProfile;
            }
            MessageBox.Show(message, "User Data Restore Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (nonewprofile)
                MessageBox.Show("Restore operations with a status of 'NoNewProfile' cannot be competed until the program is launched for the first time.");
        }
        private void CheckForSync(object source, System.Timers.ElapsedEventArgs e) {
            status = odStatus.GetStatus().Where(s => s.LocalPath == Program.OneDriveRoot).First();
            if (status.StatusString == "Up to date")
                BackupComplete();
        }
        private void BackupComplete() {
            Cursor = Cursors.Default;
            timer.Enabled = false;
            timer.Stop();
            timer.Dispose();
            string message = "Sync complete!";
            MessageBox.Show(message, "Sync Complete!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            statusLabel.Text = "Sync complete!";
            statusImage.Image = Properties.Resources.SyncComplete2;
            statusImage.Refresh();
        }

        private void Backup_Click(object sender, EventArgs e) {
            RunBackup();
        }

        private void Restore_Click(object sender, EventArgs e) {
            RunRestore();
        }
    }
}
