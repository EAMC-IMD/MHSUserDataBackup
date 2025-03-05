using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OneDrive;
using OneDrive.OdSyncService;
using UserDataBackup.Classes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace UserDataBackup.Forms {
    public partial class Main : Form {
        private ProfileData _data = new ProfileData(Program.OneDriveRoot, Program.BackupRoot);
        private OneDriveStatus _odStatus = new OneDriveStatus();
        private StatusDetail _status;
        private System.Timers.Timer _timer = new System.Timers.Timer();
        private int _timerResetCount = 0;
        private readonly int MAX_RESET_COUNT = 15;
        private readonly bool UNATTENDED_BACKUP = Environment.GetCommandLineArgs().Contains("silentback");
        private readonly bool UNATTENDED_RESTORE = Environment.GetCommandLineArgs().Contains("silentrest");
        private readonly bool UNATTENDED = Environment.GetCommandLineArgs().Contains("silentback") || Environment.GetCommandLineArgs().Contains("silentrest");
        public Main() {
            _status = _odStatus.GetStatus().First(s => s.LocalPath == Program.OneDriveRoot);
            InitializeComponent();
        }
        protected override void SetVisibleCore(bool value) {
            base.SetVisibleCore(!UNATTENDED);
        }
        private DialogResult AttendedMessageBox(string text, string caption = "", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1, MessageBoxOptions options = 0) {
            if (!UNATTENDED)
                return MessageBox.Show(text, caption, buttons, icon, defaultButton, options);
            return DialogResult.None;
        }
        private void Main_Load(object sender, EventArgs e) {
            if (!Directory.Exists(Program.OneDriveRoot)) {
                if (UNATTENDED)
                    Application.Exit();
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
                    AttendedMessageBox(Properties.Resources.NoOneDriveFolderMessage);
                else
                    AttendedMessageBox(Properties.Resources.NoOneDriveFolderMessageDISA);
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
            if (UNATTENDED_BACKUP)
                RunBackup();
            else if (UNATTENDED_RESTORE)
                RunRestore();
            if (UNATTENDED)
                Application.Exit();
        }
        private void RunBackup() {
            if (Process.GetProcessesByName("OneDrive").Count() == 0)
                Process.Start(Properties.Resources.OneDriveExecutablePath);
            _odStatus = new OneDriveStatus {
                IncludeLog = true
            };
            _status = _odStatus.GetStatus().First(s => s.LocalPath == Program.OneDriveRoot);
            if (_status.StatusString == Properties.Resources.NoNetworkStatus && !UNATTENDED) {
                AttendedMessageBox(Properties.Resources.NoNetworkMessage, "Offline Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            if (AttendedMessageBox(Properties.Resources.ProcessCloseMessage, "Process Exit Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                return;
            statusLabel.Text = Properties.Resources.BackupInProgressStatus;
            _data.Backup();
            if (_odStatus.GetStatus().First(s => s.LocalPath == Program.OneDriveRoot).StatusString == "No internet connection")
                return;
            statusLabel.Text = Properties.Resources.SyncInProgressStatus;
            AttendedMessageBox(Properties.Resources.SyncInProgressMessage, "Sync In Progress", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _status = _odStatus.GetStatus().First(s => s.LocalPath == Program.OneDriveRoot);
            statusImage.Image = Properties.Resources.SyncInPrgress;
            statusImage.Refresh();
            if (_status.StatusString != "Up to date" && !UNATTENDED) {
                _timerResetCount = 0;
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
                nonewprofile ^= result.Value.RestoreResult == RestoreResult.NoNewProfile;
            }
            AttendedMessageBox(message.ToString(), "User Data Restore Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (nonewprofile)
                AttendedMessageBox(Properties.Resources.NoNewProfileMessage);
        }
        private void CheckForSync(object source, System.Timers.ElapsedEventArgs e) {
            _timerResetCount++;
            Debug.WriteLine($"Loop {_timerResetCount} :: {DateTime.Now}");
            _status = _odStatus.GetStatus().First(s => s.LocalPath == Program.OneDriveRoot);
            foreach (StatusDetail s in _odStatus.GetStatus()) {
                foreach (System.Reflection.PropertyInfo prop in s.GetType().GetProperties()) {
                    var type = prop.PropertyType;
                    if (type == typeof(string))
                        Debug.Print(prop.GetValue(s, null).ToString());
                }
            }
            if (_status.StatusString == "Up to date" || _status.StatusString == "Synced" || _timerResetCount >= MAX_RESET_COUNT)
                BackupComplete(_timerResetCount >= MAX_RESET_COUNT);
        }
        private void BackupComplete(bool TimedOut = false) {
            Cursor = Cursors.Default;
            _timer.Enabled = false;
            _timer.Stop();
            _timer.Dispose();
            AttendedMessageBox(
                TimedOut ? Properties.Resources.SyncTimedOutMessage : Properties.Resources.SyncCompleteMessage, 
                Properties.Resources.SyncCompleteMessage, 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Information, 
                MessageBoxDefaultButton.Button1, 
                MessageBoxOptions.DefaultDesktopOnly
            );
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
