using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OneDrive;
using OneDrive.OdSyncService;

namespace UserDataBackup {
    public partial class Main : Form {
        private ProfileData _data = new ProfileData(Program.OneDriveRoot, Program.BackupRoot);
        private OneDriveStatus _odStatus = new OneDriveStatus();
        private StatusDetail _status;
        private System.Timers.Timer _timer = new System.Timers.Timer();
        public Main() {
            _status = _odStatus.GetStatus().First(s => s.LocalPath == Program.OneDriveRoot);
            InitializeComponent();
        }
        private void Main_Load(object sender, EventArgs e) {
            if (!Directory.Exists(Program.OneDriveRoot)) {
                backup.Enabled = false;
                restore.Enabled = false;
                statusLabel.Text = Properties.Resources.NoOneDriveFolderStatus;
                statusImage.Image = Properties.Resources.OneDriveError;
                statusImage.Refresh();
                string suffix = System.Net.NetworkInformation.NetworkInterface
                    .GetAllNetworkInterfaces()
                    .ToList()
                    .Where(i => i.GetIPProperties().DnsSuffix != "")
                    .First()
                    .GetIPProperties()
                    .DnsSuffix;
                if (suffix == "med.ds.osd.mil")
                    MessageBox.Show(Properties.Resources.NoOneDriveFolderMessage);
                else
                    MessageBox.Show(Properties.Resources.NoOneDriveFolderMessageDISA);
                if (Process.GetProcessesByName("OneDrive").Count() == 0)
                    Process.Start(Properties.Resources.OneDriveExecutablePath);
            } else {
                _data = new ProfileData(Program.OneDriveRoot, Program.BackupRoot);
                _odStatus = new OneDriveStatus();
                _status = _odStatus.GetStatus().First(s => s.LocalPath == Program.OneDriveRoot);
                if (_status.StatusString == Properties.Resources.NoNetworkStatus) {
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
                Process.Start(Properties.Resources.OneDriveExecutablePath);
            _odStatus = new OneDriveStatus {
                IncludeLog = true
            };
            _status = _odStatus.GetStatus().First(s => s.LocalPath == Program.OneDriveRoot);
            if (_status.StatusString == Properties.Resources.NoNetworkStatus) {
                MessageBox.Show(Properties.Resources.NoNetworkMessage, "Offline Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            if (MessageBox.Show(Properties.Resources.ProcessCloseMessage, "Process Exit Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                return;
            statusLabel.Text = Properties.Resources.BackupInProgressStatus;
            _data.Backup();
            if (_odStatus.GetStatus().First(s => s.LocalPath == Program.OneDriveRoot).StatusString == "No internet connection")
                return;
            statusLabel.Text = Properties.Resources.SyncInProgressStatus;
            MessageBox.Show(Properties.Resources.SyncInProgressMessage, "Sync In Progress", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            StringBuilder message = new StringBuilder();
            message.AppendLine(Properties.Resources.RestoreResultMessage);
            bool nonewprofile = false;
            foreach (var result in _data.Targets) {
                message.AppendLine($"\t{result.Key} : {result.Value.RestoreResult}");
                nonewprofile ^= result.Value.RestoreResult == ProfileData.RestoreResult.NoNewProfile;
            }
            MessageBox.Show(message.ToString(), "User Data Restore Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (nonewprofile)
                MessageBox.Show(Properties.Resources.NoNewProfileMessage);
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
            MessageBox.Show(Properties.Resources.SyncCompleteMessage, Properties.Resources.SyncCompleteMessage, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            statusLabel.Text = Properties.Resources.SyncCompleteMessage;
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
