using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OneDrive;
using OneDrive.OdSyncService;

namespace UserDataBackup {
    public partial class Main : Form {
        private ProfileData _data;
        private OneDriveStatus _odStatus;
        private StatusDetail _status;
        private System.Timers.Timer _timer;
        public Main() {
            InitializeComponent();
        }
        private void Main_Load(object sender, EventArgs e) {
            if (!Directory.Exists(Program.OneDriveRoot)) {
                backup.Enabled = false;
                restore.Enabled = false;
                statusLabel.Text = "This version of User Data Management is intended for OneDrive only.";
                statusImage.Image = Properties.Resources.OneDriveError;
                statusImage.Refresh();
                MessageBox.Show(
                    @"If you need assistance setting up OneDrive, please contact GSC at 800-600-9332 or https://gsc.health.mil");
                if (Process.GetProcessesByName("OneDrive").Count() == 0)
                    Process.Start(@"C:\Program Files\Microsoft OneDrive\OneDrive.exe");
            } else {
                _data = new ProfileData(Program.OneDriveRoot, Program.BackupRoot);
                _odStatus = new OneDriveStatus();
                _status = _odStatus.GetStatus().First(s => s.LocalPath == Program.OneDriveRoot);
                if (_status.StatusString == "No internet connection") {
                    statusImage.Image = Properties.Resources.offline_icon_19;
                } else {
                    statusImage.Image = Properties.Resources.onedrive_base;
                    statusImage.Image = Properties.Resources.SyncComplete2;
                }
                statusImage.Refresh();
            }
        }
        private void RunBackup() {
            if (Process.GetProcessesByName("OneDrive").Count() == 0)
                Process.Start(@"C:\Program Files\Microsoft OneDrive\OneDrive.exe");
            _odStatus = new OneDriveStatus {
                IncludeLog = true
            };
            _status = _odStatus.GetStatus().First(s => s.LocalPath == Program.OneDriveRoot);
            string message;
            if (_status.StatusString == "No internet connection") {
                message = "This program backs data up to OneDrive.  However, you do not currently have an active network connection. " +
                    "You can still run the backup, and it will be queued for update to OneDrive next time you connect. You should NOT treat this " +
                    "backup as complete until you are connected back to the MHS network, and your OneDrive icon shows 'Up to Date'.";
                MessageBox.Show(message, "Offline Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            message = "In order to complete the backup, Outlook, Sticky Notes, AsUType and all browsers must be closed. Please ensure you have no unsaved work before continuing.";
            if (MessageBox.Show(message, "Process Exit Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                return;
            statusLabel.Text = "Backup in progress.";
            _data.Backup();
            if (_odStatus.GetStatus().First(s => s.LocalPath == Program.OneDriveRoot).StatusString == "No internet connection")
                return;
            statusLabel.Text = "Sync in progress.";
            message = "Your bookmarks, sticky notes, Outlook signatures, and AsUType shortcuts (if extant) have been backed up to the Backup folder in your OneDrive.  " +
                "However, it is critical you do not log out, remove your CAC, reboot, or interrupt your network connection until sync is complete.";
            MessageBox.Show(message, "Sync In Progress", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _status = _odStatus.GetStatus().First(s => s.LocalPath == Program.OneDriveRoot);
            statusImage.Image = Properties.Resources.SyncInPrgress;
            statusImage.Refresh();
            if (_status.StatusString != "Up to date") {
                Cursor = Cursors.WaitCursor;
                _timer = new System.Timers.Timer(1000);
                _timer.Elapsed += CheckForSync;
                _timer.AutoReset = true;
                _timer.Enabled = true;
                _timer.SynchronizingObject = this;
                _timer.Start();
            }

        } 
        private void RunRestore() {
            Cursor = Cursors.WaitCursor;
            _data.Restore();
            Cursor = Cursors.Default;
            string message = "Restore operations complete! Here are the results of each item:" + Environment.NewLine;
            bool nonewprofile = false;
            foreach (var result in _data.Targets) {
                message += $"\t{result.Key} : {result.Value.RestoreResult}" + Environment.NewLine;
                nonewprofile ^= result.Value.RestoreResult == ProfileData.RestoreResult.NoNewProfile;
            }
            MessageBox.Show(message, "User Data Restore Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (nonewprofile)
                MessageBox.Show("Restore operations with a status of 'NoNewProfile' cannot be competed until the program is launched for the first time.");
        }
        private void CheckForSync(object source, System.Timers.ElapsedEventArgs e) {
            _status = _odStatus.GetStatus().First(s => s.LocalPath == Program.OneDriveRoot);
            foreach (StatusDetail s in _odStatus.GetStatus()) {
                foreach (System.Reflection.PropertyInfo prop in s.GetType().GetProperties()) {
                    var type = prop.PropertyType;
                    if (type == typeof(string))
                        Debug.Print(prop.GetValue(s, null).ToString());
                }
            }
            if (_status.StatusString == "Up to date" || _status.StatusString == "Synced")
                BackupComplete();
        }
        private void BackupComplete() {
            Cursor = Cursors.Default;
            _timer.Enabled = false;
            _timer.Stop();
            _timer.Dispose();
            MessageBox.Show("Sync complete!", "Sync Complete!", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
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
