using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using ChromiumBookmarkManager;
using System.Text.RegularExpressions;


#nullable enable
namespace UserDataBackup {
    class ProfileData {
        public enum RestoreResult {
            NotAttempted,
            NoBackupExists,
            RestoreComplete,
            NoNewProfile,
            RestoreMerge,
            MergeFailed
        }
        public string ProfileRoot { get; }
        public string BackupRoot { get; }
        public Dictionary<string, RestoreResult> RestoreResults { get; internal set; }

        private struct Browser {
            public string IndexName;
            public string Bookmark_Path;
            public string Backup_Path;
            public string? LiveBackup_Path;
            public FileInfo Live_Bookmark_File;
            public FileInfo Backup_Bookmark_File;
            public string ProcessName;
        }
        public struct BackupTarget {
            public FileSystemInfo CheckPath;
            public RestoreResult RestoreResult;
            public bool TargetExists;
        }

        private Browser chrome;
        private Browser edge;
        private Browser firefox;

        private static readonly string localappdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public Dictionary<string, BackupTarget> Targets { get; set; }

        public ProfileData(string profileRoot, string backupRoot) {
            ProfileRoot = profileRoot;
            BackupRoot = backupRoot;

            chrome = new Browser {
                IndexName = "Chrome",
                Bookmark_Path = $@"{localappdata}\Google\Chrome\User Data\Default\Bookmarks",
                Backup_Path = $@"{BackupRoot}\Chrome\Bookmarks",
                LiveBackup_Path = $@"{localappdata}\Google\Chrome\User Data\Default\Bookmarks.bak",
                Live_Bookmark_File = new FileInfo($@"{localappdata}\Google\Chrome\User Data\Default\Bookmarks"),
                Backup_Bookmark_File = new FileInfo($@"{BackupRoot}\Chrome\Bookmarks"),
                ProcessName = "chrome"
            };
            edge = new Browser {
                IndexName = "Edge",
                Bookmark_Path = $@"{localappdata}\Microsoft\Edge\User Data\Default\Bookmarks",
                Backup_Path = $@"{BackupRoot}\Edge\Bookmarks",
                LiveBackup_Path = $@"{localappdata}\Microsoft\Edge\User Data\Default\Bookmarks.bak",
                Live_Bookmark_File = new FileInfo($@"{localappdata}\Microsoft\Edge\User Data\Default\Bookmarks"),
                Backup_Bookmark_File = new FileInfo($@"{BackupRoot}\Edge\Bookmarks"),
                ProcessName = "msedge"
            };
            firefox = new Browser {
                IndexName = "Firefox",
                Bookmark_Path = $@"{appdata}\Mozilla\Firefox\",
                Backup_Path = $@"{BackupRoot}\Firefox\",
                LiveBackup_Path = null,
                Live_Bookmark_File = new FileInfo($@"{appdata}\Mozilla\Firefox\profiles.ini"),
                Backup_Bookmark_File = new FileInfo($@"{BackupRoot}\Firefox\profiles.ini"),
                ProcessName = "firefox"
            };
            Targets = new Dictionary<string, BackupTarget> {
                ["Chrome"] = new BackupTarget {
                    CheckPath = chrome.Live_Bookmark_File,
                    RestoreResult = RestoreResult.NotAttempted,
                    TargetExists = false
                },
                ["Edge"] = new BackupTarget {
                    CheckPath = edge.Live_Bookmark_File,
                    RestoreResult = RestoreResult.NotAttempted,
                    TargetExists = false
                },
                ["Firefox"]  = new BackupTarget {
                    CheckPath = firefox.Live_Bookmark_File,
                    RestoreResult = RestoreResult.NotAttempted,
                    TargetExists = false
                },
                ["StickyNotes"] = new BackupTarget {
                    CheckPath = new FileInfo($@"{localappdata}\Packages\Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe\LocalState\plum.sqlite"),
                    RestoreResult = RestoreResult.NotAttempted,
                    TargetExists = false
                },
                ["Outlook Signatures"] = new BackupTarget {
                    CheckPath = new DirectoryInfo($@"{appdata}\Microsoft\Signatures"),
                    RestoreResult = RestoreResult.NotAttempted,
                    TargetExists = false
                },
                ["AsUType"] = new BackupTarget {
                    CheckPath = new DirectoryInfo($@"C:\Users\Public\Documents\Fanix"),
                    RestoreResult = RestoreResult.NotAttempted,
                    TargetExists = false
                }
            };
            RestoreResults = new Dictionary<string, RestoreResult>();
            CheckTargets();
        }
        private static void CopyDir(string sourcePath, string targetPath, bool recursive) {
            CopyDir(sourcePath, targetPath, null, recursive);
        }
        private static void CopyDir(string sourcePath, string targetPath, string? filter, bool recursive) {
            DirectoryInfo sourceInfo = new DirectoryInfo(sourcePath);
            if (Regex.IsMatch(sourceInfo.FullName, @"Firefox\\Profiles\\.*\\storage\\default"))
                return;
            if (!sourceInfo.Exists)
                throw new DirectoryNotFoundException();
            DirectoryInfo[] subSource = sourceInfo.GetDirectories();
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);
            foreach (FileInfo file in filter is null?sourceInfo.GetFiles():sourceInfo.GetFiles(filter)) {
                string filePath = Path.Combine(targetPath, file.Name);
                file.CopyTo(filePath, true);
            }
            if (recursive) {
                foreach (DirectoryInfo subDir in subSource) {
                    string newTarget = Path.Combine(targetPath, subDir.Name);
                    CopyDir(subDir.FullName, newTarget, true);
                }
            }
        }
        public void CheckTargets() {
            List<string> keys = new List<string>(Targets.Keys);
            foreach (string key in keys) {
                Targets[key] = new BackupTarget {
                    CheckPath = Targets[key].CheckPath,
                    RestoreResult = Targets[key].RestoreResult,
                    TargetExists = Targets[key].CheckPath.Exists
                };
            }
        }
        public void Backup() {
            BackupChrome();
            BackupEdge();
            BackupFirefox();
            BackupStickyNotes();
            BackupSignatures();
            BackupAsUType();
        }
        public bool Restore() {
            RestoreChrome();
            RestoreFirefox();
            RestoreEdge();
            RestoreStickyNotes();
            RestoreSignatures();
            int failCount = RestoreResults.Count(kv => kv.Value.Equals(RestoreResult.NoNewProfile));
            failCount += RestoreResults.Count(kv => kv.Value.Equals(RestoreResult.MergeFailed));
            if (failCount > 0)
                return false;
            return true;
        }
        private void BackupChromium(Browser target) {
            if (!Targets[target.IndexName].TargetExists)
                return;
            if (!target.Backup_Bookmark_File.Directory.Exists)
                target.Backup_Bookmark_File.Directory.Create();
            foreach (Process process in Process.GetProcessesByName(target.ProcessName)) {
                if (!process.HasExited) {
                    process.Kill();
                    process.WaitForExit();
                }
            }
            target.Live_Bookmark_File.CopyTo(target.Backup_Path, true);
        }
        private void BackupChrome() {
            BackupChromium(chrome);
        }
        private void BackupEdge() {
            BackupChromium(edge);
        }
        private void BackupFirefox() {
            if (!Targets[firefox.IndexName].TargetExists)
                return;
            if (!Directory.Exists(firefox.Backup_Path))
                Directory.CreateDirectory(firefox.Backup_Path);
            foreach (Process process in Process.GetProcessesByName(firefox.ProcessName)) {
                try {
                    process.Kill();
                    process.WaitForExit();
                } catch { }
            }
            CopyDir(firefox.Bookmark_Path, firefox.Backup_Path, true);
        }
        private void BackupStickyNotes() {
            BackupTarget thisTarget = Targets["StickyNotes"];
            if (!thisTarget.TargetExists)
                return;
            FileInfo stickyBackup = new FileInfo($@"{BackupRoot}\StickyNotes\plum.sqlite");
            if (!stickyBackup.Directory.Exists) {
                stickyBackup.Directory.Create();
            }
            foreach (Process process in Process.GetProcessesByName("Microsoft.Notes")) {
                process.Kill();
                process.WaitForExit();
            }
            ((FileInfo)thisTarget.CheckPath).CopyTo(stickyBackup.FullName, true);
        }
        private void BackupSignatures() {
            BackupTarget thisTarget = Targets["Outlook Signatures"];
            if (!thisTarget.TargetExists)
                return;
            DirectoryInfo sigBackup = new DirectoryInfo($@"{BackupRoot}\Signatures");
            if (!sigBackup.Exists)
                Directory.CreateDirectory(sigBackup.FullName);
            foreach (Process process in Process.GetProcessesByName("OUTLOOK")) {
                process.Kill();
                process.WaitForExit();
            }
            CopyDir(thisTarget.CheckPath.FullName, sigBackup.FullName, true);
        }
        private void BackupAsUType() {
            BackupTarget thisTarget = Targets["AsUType"];
            if (!thisTarget.TargetExists)
                return;
            DirectoryInfo autBackup = new DirectoryInfo($@"{BackupRoot}\AsUType");
            if (!autBackup.Exists)
                Directory.CreateDirectory(autBackup.FullName);
            foreach (Process process in Process.GetProcessesByName("asutype")) {
                process.Kill();
                process.WaitForExit();
            }
            CopyDir(thisTarget.CheckPath.FullName, autBackup.FullName, "*.shortcut", false);
        }
        private void RestoreChromium(Browser target) {
            BackupTarget local = Targets[target.IndexName];
            if (!target.Backup_Bookmark_File.Exists) {
                local.RestoreResult = RestoreResult.NoBackupExists;
                Targets[target.IndexName] = local;
                return;
            }
            if (!target.Live_Bookmark_File.Directory.Exists) {
                local.RestoreResult = RestoreResult.NoNewProfile;
                Targets[target.IndexName] = local;
                return;
            }
            if (!target.Live_Bookmark_File.Exists) {
                foreach (Process process in Process.GetProcessesByName(target.ProcessName)) {
                    process.Kill();
                    process.WaitForExit();
                }
                target.Backup_Bookmark_File.CopyTo(target.Bookmark_Path);
                RestoreResults[target.IndexName] = RestoreResult.RestoreComplete;
                return;
            }
            BookmarkFile live = new BookmarkFile(target.Bookmark_Path);
            BookmarkFile backup = new BookmarkFile(target.Backup_Path);
            foreach (Process process in Process.GetProcessesByName(target.ProcessName)) {
                process.Kill();
                process.WaitForExit();
            }
            try {
                BookmarkFile merge = new BookmarkFile();
                if (!live.Merge(backup, out merge)) {
                    RestoreResults[target.IndexName] = RestoreResult.MergeFailed;
                    return;
                }
                merge.WriteFile(live.FilePath);
                FileInfo live_bookmarkbackup = new FileInfo(target.LiveBackup_Path);
                live_bookmarkbackup.Delete();
                local.RestoreResult = RestoreResult.RestoreComplete;
            } catch {
                local.RestoreResult = RestoreResult.MergeFailed;
            } finally {
                Targets[target.IndexName] = local;
            }
        }
        private void RestoreChrome() {
            RestoreChromium(chrome);
        }
        private void RestoreEdge() {
            RestoreChromium(edge);
        }
        private void RestoreFirefox() {
            BackupTarget local = Targets[firefox.IndexName];
            if (!firefox.Backup_Bookmark_File.Exists) {
                local.RestoreResult = RestoreResult.NoBackupExists;
                Targets[firefox.IndexName] = local;
                return;
            }
            if (!Directory.Exists(firefox.Bookmark_Path))
                Directory.CreateDirectory(firefox.Bookmark_Path);
            foreach (Process process in Process.GetProcessesByName(firefox.ProcessName)) {
                process.Kill();
                process.WaitForExit();
            }
            CopyDir(firefox.Backup_Path, firefox.Bookmark_Path, true);
            local.RestoreResult = RestoreResult.RestoreComplete;
            Targets[firefox.IndexName] = local;
        }
        private void RestoreStickyNotes() {
            BackupTarget local = Targets["StickyNotes"];
            FileInfo stickyfile = new FileInfo($@"{localappdata}\Packages\Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe\LocalState\plum.sqlite");
            FileInfo stickyBackup = new FileInfo($@"{BackupRoot}\StickyNotes\plum.sqlite");
            if (!stickyBackup.Exists) {
                local.RestoreResult = RestoreResult.NoBackupExists;
                Targets["StickyNotes"] = local;
                return;
            }
            if (!Directory.Exists(stickyfile.Directory.FullName))
                Directory.CreateDirectory(stickyfile.Directory.FullName);
            foreach (Process process in Process.GetProcessesByName("Microsoft.Notes")) {
                process.Kill();
                process.WaitForExit();
            }
            stickyBackup.CopyTo(stickyfile.FullName, true);
            local.RestoreResult = RestoreResult.RestoreComplete;
            Targets["StickyNotes"] = local;
        }
        private void RestoreSignatures() {
            BackupTarget local = Targets["Outlook Signatures"];
            DirectoryInfo sigFolder = new DirectoryInfo($@"{appdata}\Microsoft\Signatures");
            DirectoryInfo sigBackup = new DirectoryInfo($@"{BackupRoot}\Signatures");
            if (!sigBackup.Exists) {
                local.RestoreResult = RestoreResult.NoBackupExists;
                Targets["Outlook Signatures"] = local;
                return;
            }
            if (!sigFolder.Exists)
                Directory.CreateDirectory(sigFolder.FullName);
            foreach (Process process in Process.GetProcessesByName("OUTLOOK")) {
                process.Kill();
                process.WaitForExit();
            }
            CopyDir(sigBackup.FullName, sigFolder.FullName, true);
            local.RestoreResult = RestoreResult.RestoreComplete;
            Targets["Outlook Signatures"] = local;
        }
    }
}
