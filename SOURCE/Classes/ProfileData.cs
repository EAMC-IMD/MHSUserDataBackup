using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using SapphTools.BookmarkManager.Chromium;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Text.Json;
using System.Reflection;
using System.Text;
using System.Security;

#nullable enable
namespace UserDataBackup.Classes {
    class ProfileData {
        public string ProfileRoot { get; }
        public string BackupRoot { get; }

        private static readonly string _localappdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly string _appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public TargetCollection BackupTargets = new TargetCollection();

        public ProfileData(string profileRoot, string backupRoot) {
            ProfileRoot = profileRoot;
            BackupRoot = backupRoot;
            string jsonPath = $@"{Path.GetDirectoryName(Application.ExecutablePath)}\BackupTargets.json";
            string jsonText = File.ReadAllText(jsonPath);
            if (jsonText is null) {
                var assembly = Assembly.GetExecutingAssembly();
                jsonText = Encoding.UTF8.GetString(Properties.Resources.BackupTargets);
            }
            BackupTargets = JsonSerializer.Deserialize<TargetCollection>(jsonText)!;
            CheckTargets();
        }
        private static void CopyDir(string sourcePath, string targetPath, bool recursive)  {
            CopyDir(sourcePath, targetPath, null, recursive);
        }
        private static void CopyDir(string sourcePath, string targetPath, string? filter, bool recursive) {
            CopyDir(sourcePath, targetPath, filter, null, recursive);
        }
        private static void CopyDir(string sourcePath, string targetPath, string? filter, string? exclusionFilter, bool recursive) {
            DirectoryInfo sourceInfo = new DirectoryInfo(sourcePath);
            if (Regex.IsMatch(sourceInfo.FullName, @"Firefox\\Profiles\\.*\\storage\\default"))
                return;
            if (!sourceInfo.Exists)
                throw new DirectoryNotFoundException();
            DirectoryInfo[] subSource = sourceInfo.GetDirectories();
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);
            foreach (FileInfo file in filter is null ? sourceInfo.GetFiles() : sourceInfo.GetFiles(filter)) {
                if (exclusionFilter != null && Regex.IsMatch(file.Name, exclusionFilter))
                    continue;
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
            foreach (BackupTarget target in BackupTargets) {
                target.Validate();
            }
        }
        private void KillProcessTree(string ProcessName) {
            if (ProcessName == null)
                return;
            foreach (Process process in Process.GetProcessesByName(ProcessName)) {
                try {
                    if (!process.HasExited) {
                        process.Kill();
                        process.WaitForExit();
                    }
                } catch { }
            }
        }
        private void KillProcessTree(List<string> ProcessNames) {
            foreach (string ProcessName in ProcessNames) { 
                KillProcessTree(ProcessName); 
            }
        }
        public void Backup() {
            foreach (BackupTarget target in BackupTargets) {
                switch (target.Type) {
                    case TargetType.Chrome when target is BrowserTarget browserTarget:
                        BackupChrome(browserTarget);
                        break;
                    case TargetType.Edge when target is BrowserTarget browserTarget:
                        BackupEdge(browserTarget);
                        break;
                    case TargetType.Firefox when target is BrowserTarget browserTarget:
                        BackupFirefox(browserTarget);
                        break;
                    case TargetType.StickyNotes when target is ApplicationTarget appTarget:
                        BackupStickyNotes(appTarget);
                        break;
                    case TargetType.OutlookSigs when target is ApplicationTarget appTarget:
                        BackupSignatures(appTarget);
                        break;
                    case TargetType.AsUType when target is ApplicationTarget appTarget:
                        BackupAsUType(appTarget);
                        break;
                    case TargetType.Npp when target is ApplicationTarget appTarget:
                        BackupNpp(appTarget);
                        break;
                    case TargetType.AutoDest when target is ApplicationTarget appTarget:
                        BackupAutoDest(appTarget);
                        break;
                    default:
                    case TargetType.Other:
                        BackupDefault(target);
                        break;
                }
            }
        }
        public bool Restore() {
            if (!Directory.Exists(BackupRoot))
                return false;
            foreach (BackupTarget target in BackupTargets) {
                switch (target.Type) {
                    case TargetType.Chrome when target is BrowserTarget browserTarget:
                        RestoreChrome(browserTarget);
                        break;
                    case TargetType.Edge when target is BrowserTarget browserTarget:
                        RestoreEdge(browserTarget);
                        break;
                    case TargetType.Firefox when target is BrowserTarget browserTarget:
                        RestoreFirefox(browserTarget);
                        break;
                    case TargetType.StickyNotes when target is ApplicationTarget appTarget:
                        RestoreStickyNotes(appTarget);
                        break;
                    case TargetType.OutlookSigs when target is ApplicationTarget appTarget:
                        RestoreSignatures(appTarget);
                        break;
                    case TargetType.AsUType when target is ApplicationTarget appTarget:
                        RestoreAsUType(appTarget);
                        break;
                    case TargetType.Npp when target is ApplicationTarget appTarget:
                        RestoreNpp(appTarget);
                        break;
                    case TargetType.AutoDest when target is ApplicationTarget appTarget:
                        RestoreAutoDest(appTarget);
                        break;
                    default:
                    case TargetType.Other:
                        RestoreDefault(target);
                        break;
                }
            }
            int failCount = BackupTargets.RestoreResults.Count(kv => kv.Value.Equals(RestoreResult.NoNewProfile));
            failCount += BackupTargets.RestoreResults.Count(kv => kv.Value.Equals(RestoreResult.MergeFailed));
            if (failCount > 0)
                return false;
            return true;
        }
        private void BackupChromium(BrowserTarget target) {
            if (!target.TargetExists || target.BrowserInfo is null)
                return;
            if (!target.BrowserInfo.Backup_Bookmark_File.Directory.Exists)
                target.BrowserInfo.Backup_Bookmark_File.Directory.Create();
            KillProcessTree(target.ProcessName);
            try {
                target.BrowserInfo.Live_Bookmark_File.CopyTo(target.BackupFolder, true);
            } catch (ArgumentNullException) {
                Debug.WriteLine("BrowserInfo.LiveBookmark_Path was null");
            } catch (Exception e) when (e is SecurityException || e is UnauthorizedAccessException) {
                Debug.WriteLine($"Access denied to {target.BrowserInfo.LiveBackup_Path}");
            } catch (Exception e) when (e is ArgumentException || e is PathTooLongException || e is NotSupportedException) {
                Debug.WriteLine($"LiveBackup_Path contained invalid data : {target.BrowserInfo.LiveBackup_Path}");
            }
        }
        private void BackupChrome(BrowserTarget target) {
            BackupChromium(target);
        }
        private void BackupEdge(BrowserTarget target) {
            BackupChromium(target);
        }
        private void BackupFirefox(BrowserTarget target) {
            if (!target.TargetExists || target.BrowserInfo is null)
                return;
            if (target.BrowserInfo.Bookmark_Path is null || target.BrowserInfo.Backup_Path is null)
                return;
            if (!Directory.Exists(target.BackupFolder))
                Directory.CreateDirectory(target.BackupFolder);
            KillProcessTree(target.ProcessName);
            CopyDir(target.BrowserInfo.Bookmark_Path, target.BrowserInfo.Backup_Path, null, @"\.com$", true);
        }
        private void BackupStickyNotes(ApplicationTarget target) {
            if (!target.TargetExists || target.CheckPathInfo is null)
                return;
            FileInfo stickyBackup = new FileInfo($@"{BackupRoot}\{target.BackupFolder}\plum.sqlite");
            if (!stickyBackup.Directory.Exists) {
                stickyBackup.Directory.Create();
            }
            KillProcessTree(target.ProcessName);
            (target.CheckPathInfo).CopyTo(stickyBackup.FullName, true);
        }
        private void BackupSignatures(ApplicationTarget target) {
            if (!target.TargetExists || target.CheckPathInfo is null)
                return;
            DirectoryInfo sigBackup = new DirectoryInfo($@"{BackupRoot}\{target.BackupFolder}");
            if (!sigBackup.Exists)
                Directory.CreateDirectory(sigBackup.FullName);
            KillProcessTree(target.ProcessName);
            CopyDir(target.CheckPath, sigBackup.FullName, true);
        }
        private void BackupAsUType(ApplicationTarget target) {
            if (!target.TargetExists || target.CheckPathInfo is null)
                return;
            DirectoryInfo autBackup = new DirectoryInfo($@"{BackupRoot}\{target.BackupFolder}");
            if (!autBackup.Exists)
                Directory.CreateDirectory(autBackup.FullName);
            KillProcessTree(target.ProcessName);
            string[] config = File.ReadAllLines($@"{_localappdata}\Fanix\Asutype Professional\asutype.config", Encoding.Unicode);
            string user = Environment.UserName;
            List<string> globalSpellers = new List<string> {
                "english.spelling",
                "medical.spelling",
                "mhs_terms.spelling",
                "ranks_us_army.spelling"
            };
            string dataFolder = "";
            for (int i = 0; i < config.Length; i++) {
                Regex FolderRegex = new Regex(@"(?:DataFolder\s=\s.*,\s)(?<path>.*)");
                Regex CorrectorRegex = new Regex(@"(?<Header>CorrectorMyFileList\s=\s.*,\s)(?<filegroup>((?:\.\\)(?<filename>[^|\\]*\.correction)(?:\|?))+)");
                Regex ExpanderRegex = new Regex(@"(?<Header>ExpanderMyFileList\s=\s.*,\s)(?<filegroup>((?:\.\\)(?<filename>[^|\\]*\.shortcut)(?:\|?))+)");
                Regex SpellerRegex = new Regex(@"(?<Header>SpellerMyFileList\s=\s.*,\s)(?<filegroup>((?:\.\\)(?<filename>[^|\\]*\.spelling)(?:\|?))+)");
                MatchCollection FolderMatches = FolderRegex.Matches(config[i]);
                MatchCollection CorrectorMatches = CorrectorRegex.Matches(config[i]);
                MatchCollection ExpanderMatches = ExpanderRegex.Matches(config[i]);
                MatchCollection SpellerMatches = SpellerRegex.Matches(config[i]);
                List<string> globalFiles = new List<string>();
                List<string> personalFiles = new List<string>();
                string header = "";
                if (FolderMatches.Count > 0) {
                    dataFolder = FolderMatches[0].Groups["path"].Value;
                    continue;
                }
                if (CorrectorMatches.Count > 0) {
                    personalFiles = CorrectorMatches[0].Groups["filename"].Captures.OfType<Capture>().Select(s => s.Value).ToList<string>();
                    header = CorrectorMatches[0].Groups["Header"].Captures.OfType<Capture>().Select(s => s.Value).First<string>();
                }
                if (ExpanderMatches.Count > 0) {
                    personalFiles = ExpanderMatches[0].Groups["filename"].Captures.OfType<Capture>().Select(s => s.Value).ToList<string>();
                    header = ExpanderMatches[0].Groups["Header"].Captures.OfType<Capture>().Select(s => s.Value).First<string>();
                }
                if (SpellerMatches.Count > 0) {
                    personalFiles = SpellerMatches[0].Groups["filename"].Captures.OfType<Capture>().Select(s => s.Value).ToList<string>();
                    header = SpellerMatches[0].Groups["Header"].Captures.OfType<Capture>().Select(s => s.Value).First<string>();
                    globalFiles = new List<string>(personalFiles);
                    personalFiles.RemoveAll(f => globalSpellers.Any(g => f.Contains(g)));
                    globalFiles.RemoveAll(g => personalFiles.Any(p => g.Contains(p)));
                }
                if (personalFiles.Count == 0)
                    continue;
                string newline = $@"{header}";
                bool first = true;
                string[] FilesToRename = personalFiles.ToArray();
                for (int j = 0; j < FilesToRename.Length; j++) {
                    string thisName = personalFiles[j];
                    if (Regex.IsMatch(thisName, $@"{user}"))
                        continue;
                    string newName = String.Format("{0}-{1,0:D2}{2}", user, j + 1, Path.GetExtension(thisName));
                    File.Move($@"{dataFolder}\{thisName}", $@"{dataFolder}\{newName}");
                    personalFiles[j] = newName;
                }
                foreach (string file in personalFiles) {
                    if (!first)
                        newline += "|";
                    first = false;
                    newline += $@".\{file}";
                }
                foreach (string file in globalFiles) {
                    if (!first)
                        newline += "|";
                    first = false;
                    newline += $@".\{file}";
                }
                config[i] = newline;
            }
            File.WriteAllLines($@"{_localappdata}\Fanix\Asutype Professional\asutype.config", config, Encoding.Unicode);
            File.Copy($@"{_localappdata}\Fanix\Asutype Professional\asutype.config", $@"{BackupRoot}\AsUType\asutype.config", true);
            CopyDir(@"C:\Users\Public\Documents\Fanix\Asutype Professional", $@"{BackupRoot}\AsUType", "*.correction", false);
            CopyDir(@"C:\Users\Public\Documents\Fanix\Asutype Professional", $@"{BackupRoot}\AsUType", "*.shortcut", false);
            CopyDir(@"C:\Users\Public\Documents\Fanix\Asutype Professional", $@"{BackupRoot}\AsUType", "*.spelling", false);
        }
        private void BackupNpp(ApplicationTarget target) {
            if (!target.TargetExists || target.CheckPathInfo is null)
                return;
            if (!Directory.Exists($@"{_appdata}\Notepad++\backup"))
                return;
            string nppBackupFolder = $@"{BackupRoot}\{target.BackupFolder}";
            FileInfo nppBackup = new FileInfo($@"{nppBackupFolder}\session.xml");
            string fileTarget = $@"{nppBackupFolder}\files";
            if (!nppBackup.Directory.Exists) {
                nppBackup.Directory.Create();
                Directory.CreateDirectory(fileTarget);
            }
            KillProcessTree(target.ProcessName);
            (target.CheckPathInfo).CopyTo(nppBackup.FullName, true);
            CopyDir($@"{_appdata}\Notepad++\backup", fileTarget, false);
        }
        private void BackupAutoDest(ApplicationTarget target) {
            if (!target.TargetExists || target.CheckPathInfo is null)
                return;
            DirectoryInfo destBackup = new DirectoryInfo($@"{BackupRoot}\{target.BackupFolder}");
            if (!destBackup.Exists)
                Directory.CreateDirectory(destBackup.FullName);
            CopyDir(target.CheckPath, destBackup.FullName, false);

        }
        private void BackupDefault(BackupTarget target) {
            if (!target.TargetExists || target.CheckPathInfo is null)
                return;
            DirectoryInfo destBackup = new DirectoryInfo($@"{BackupRoot}\{target.BackupFolder}");
            if (!destBackup.Exists)
                Directory.CreateDirectory(destBackup.FullName);
            CopyDir(target.CheckPath, destBackup.FullName, false);

        }
        private void RestoreChromium(BrowserTarget target) {
            if (target.BrowserInfo is null)
                return;
            if (!target.BrowserInfo.Backup_Bookmark_File.Exists) {
                target.Result = RestoreResult.NoBackupExists;
                return;
            }
            if (!target.BrowserInfo.Live_Bookmark_File.Directory.Exists) {
                target.Result = RestoreResult.NoNewProfile;
                return;
            }
            if (!target.BrowserInfo.Live_Bookmark_File.Exists) {
                KillProcessTree(target.ProcessName);
                try {
                    target.BrowserInfo.Backup_Bookmark_File.CopyTo(target.BrowserInfo.Bookmark_Path);
                } catch (IOException) {
                    KillProcessTree(target.ProcessName);
                    try {
                        target.BrowserInfo.Backup_Bookmark_File.CopyTo(target.BrowserInfo.Bookmark_Path);
                    } catch (IOException) {
                        target.Result = RestoreResult.MergeFailed;
                        return;
                    }
                }
                target.Result = RestoreResult.RestoreComplete;
                return;
            }
            if (target.BrowserInfo.Bookmark_Path is null) {
                target.Result = RestoreResult.NoNewProfile;
                return;
            }
            if (target.BrowserInfo.Backup_Path is null) {
                target.Result = RestoreResult.NoBackupExists;
                return;
            }
            BookmarkFile live = new BookmarkFile(target.BrowserInfo.Bookmark_Path);
            BookmarkFile backup = new BookmarkFile(target.BrowserInfo.Backup_Path);
            KillProcessTree(target.ProcessName);
            try {
                if (!live.Merge(backup, out BookmarkFile merge)) {
                    target.Result = RestoreResult.MergeFailed;
                    return;
                }
                try {
                    merge.WriteFile(live.FilePath);
                    FileInfo live_bookmarkbackup = new FileInfo(target.BrowserInfo.LiveBackup_Path);
                    live_bookmarkbackup.Delete();
                } catch (IOException) {
                    KillProcessTree(target.ProcessName);
                    try {
                        merge.WriteFile(live.FilePath);
                        FileInfo live_bookmarkbackup = new FileInfo(target.BrowserInfo.LiveBackup_Path);
                        live_bookmarkbackup.Delete();
                    } catch (IOException) {
                        target.Result = RestoreResult.MergeFailed;
                        return;
                    }
                }
            } catch {
                target.Result = RestoreResult.MergeFailed;
                return;
            }
            target.Result = RestoreResult.RestoreComplete;
            return;
        }
        private void RestoreChrome(BrowserTarget target) {
            RestoreChromium(target);
        }
        private void RestoreEdge(BrowserTarget target) {
            RestoreChromium(target);
        }
        private void RestoreFirefox(BrowserTarget target) {
            if (target.BrowserInfo is null || target.BrowserInfo.Backup_Path is null || target.BrowserInfo.Bookmark_Path is null)
                return;
            if (!target.BrowserInfo.Backup_Bookmark_File.Exists) {
                target.Result = RestoreResult.NoBackupExists;
                return;
            }
            if (!Directory.Exists(target.BrowserInfo.Bookmark_Path))
                Directory.CreateDirectory(target.BrowserInfo.Bookmark_Path);
            KillProcessTree(target.ProcessName);
            try {
                CopyDir(target.BrowserInfo.Backup_Path, target.BrowserInfo.Bookmark_Path, true);
            } catch (IOException) {
                KillProcessTree(target.ProcessName);
                try {
                    CopyDir(target.BrowserInfo.Backup_Path, target.BrowserInfo.Bookmark_Path, true);
                } catch (IOException) {
                    target.Result = RestoreResult.MergeFailed;
                    return;
                }
            }

            target.Result = RestoreResult.RestoreComplete;
        }
        private void RestoreStickyNotes(ApplicationTarget target) {
            if (target.CheckPathInfo is null)
                return;
            FileInfo stickyBackup = new FileInfo($@"{BackupRoot}\{target.BackupFolder}\plum.sqlite");
            if (!stickyBackup.Exists) {
                target.Result = RestoreResult.NoBackupExists;
                return;
            }
            if (!Directory.Exists(target.CheckPathInfo.Directory.FullName))
                Directory.CreateDirectory(target.CheckPathInfo.Directory.FullName);
            stickyBackup.CopyTo(target.CheckPathInfo.FullName, true);
            KillProcessTree(target.ProcessName);
            try {
                stickyBackup.CopyTo(target.CheckPathInfo.FullName, true);
            } catch (IOException) {
                KillProcessTree(target.ProcessName);
                try {
                    stickyBackup.CopyTo(target.CheckPathInfo.FullName, true);
                } catch (IOException) {
                    target.Result = RestoreResult.MergeFailed;
                    return;
                }
            }
            target.Result = RestoreResult.RestoreComplete;
        }
        private void RestoreSignatures(ApplicationTarget target) {
            DirectoryInfo sigFolder = new DirectoryInfo(target.CheckPath);
            DirectoryInfo sigBackup = new DirectoryInfo($@"{BackupRoot}\{target.BackupFolder}");
            if (!sigBackup.Exists) {
                target.Result = RestoreResult.NoBackupExists;
                return;
            }
            if (!sigFolder.Exists)
                Directory.CreateDirectory(sigFolder.FullName);
            KillProcessTree(target.ProcessName);
            try {
                CopyDir(sigBackup.FullName, sigFolder.FullName, true);
                target.Result = RestoreResult.RestoreComplete;
            } catch (IOException) {
                KillProcessTree(target.ProcessName);
                try {
                    CopyDir(sigBackup.FullName, sigFolder.FullName, true);
                    target.Result = RestoreResult.RestoreComplete;
                } catch (IOException) {
                    target.Result = RestoreResult.MergeFailed;
                    return;
                }
            }
            target.Result = RestoreResult.RestoreComplete;
        }
        private void RestoreAsUType(ApplicationTarget target) {
            if (!File.Exists($@"{BackupRoot}\{target.BackupFolder}\asutype.config")) {
                target.Result = RestoreResult.NoBackupExists;
                return;
            }
            if (!Directory.Exists($@"{_localappdata}\Fanix\Asutype Professional\"))
                Directory.CreateDirectory($@"{_localappdata}\Fanix\Asutype Professional\");
            KillProcessTree(target.ProcessName);
            try {
                CopyDir($@"{BackupRoot}\AsUType", @"C:\Users\Public\Documents\Fanix\Asutype Professional", "*.correction", false);
                CopyDir($@"{BackupRoot}\AsUType", @"C:\Users\Public\Documents\Fanix\Asutype Professional", "*.shortcut", false);
                CopyDir($@"{BackupRoot}\AsUType", @"C:\Users\Public\Documents\Fanix\Asutype Professional", "*.spelling", false);
                File.Copy($@"{BackupRoot}\AsUType\asutype.config", $@"{_localappdata}\Fanix\Asutype Professional\asutype.config", true);
            } catch (IOException) {
                KillProcessTree(target.ProcessName);
                try {
                    CopyDir($@"{BackupRoot}\AsUType", @"C:\Users\Public\Documents\Fanix\Asutype Professional", "*.correction", false);
                    CopyDir($@"{BackupRoot}\AsUType", @"C:\Users\Public\Documents\Fanix\Asutype Professional", "*.shortcut", false);
                    CopyDir($@"{BackupRoot}\AsUType", @"C:\Users\Public\Documents\Fanix\Asutype Professional", "*.spelling", false);
                    File.Copy($@"{BackupRoot}\AsUType\asutype.config", $@"{_localappdata}\Fanix\Asutype Professional\asutype.config", true);
                } catch {
                    target.Result = RestoreResult.MergeFailed;
                    return;
                }
            }
            target.Result = RestoreResult.RestoreComplete;
        }
        private void RestoreNpp(ApplicationTarget target) {
            FileInfo nppBackupFile = new FileInfo($@"{BackupRoot}\{target.BackupFolder}\session.xml");
            if (!nppBackupFile.Exists) {
                target.Result = RestoreResult.NoBackupExists;
                return;
            }
            if (target.CheckPathInfo is null)
                return;
            if (!Directory.Exists(target.CheckPathInfo.Directory.FullName)) {
                target.CheckPathInfo.Directory.Create();
            }
            KillProcessTree(target.ProcessName); //FINISH TRYCATCH
            try {
                nppBackupFile.CopyTo(target.CheckPathInfo.FullName, true);
                DirectoryInfo nppBackupFolder = new DirectoryInfo($@"{target.CheckPathInfo.Directory.FullName}\backup");
                if (!nppBackupFolder.Exists)
                    Directory.CreateDirectory(nppBackupFolder.FullName);
                CopyDir($@"{nppBackupFile.DirectoryName}\files", nppBackupFolder.FullName, false);
            } catch (IOException) {
                KillProcessTree(target.ProcessName);
                try {
                    nppBackupFile.CopyTo(target.CheckPathInfo.FullName, true);
                    DirectoryInfo nppBackupFolder = new DirectoryInfo($@"{target.CheckPathInfo.Directory.FullName}\backup");
                    if (!nppBackupFolder.Exists)
                        Directory.CreateDirectory(nppBackupFolder.FullName);
                    CopyDir($@"{nppBackupFile.DirectoryName}\files", nppBackupFolder.FullName, false);
                } catch (IOException) {
                    target.Result = RestoreResult.MergeFailed;
                    return;
                }
            }

            target.Result = RestoreResult.RestoreComplete;
        }
        private void RestoreAutoDest(ApplicationTarget target) {
            DirectoryInfo destFolder = new DirectoryInfo(target.CheckPath);
            DirectoryInfo sourceFolder = new DirectoryInfo($@"{BackupRoot}\{target.BackupFolder}");
            if (!sourceFolder.Exists) {
                target.Result = RestoreResult.NoBackupExists;
                return;
            }
            if (!destFolder.Exists)
                Directory.CreateDirectory(destFolder.FullName);
            KillProcessTree(target.ProcessName);
            try {
                CopyDir(sourceFolder.FullName, destFolder.FullName, true);
                target.Result = RestoreResult.RestoreComplete;
            } catch (IOException) {
                KillProcessTree(target.ProcessName);
                try {
                    CopyDir(sourceFolder.FullName, destFolder.FullName, true);
                    target.Result = RestoreResult.RestoreComplete;
                } catch (IOException) {
                    target.Result = RestoreResult.MergeFailed;
                    return;
                }
            }
            target.Result = RestoreResult.RestoreComplete;
        }
        private void RestoreDefault(BackupTarget target) {
            DirectoryInfo destFolder = new DirectoryInfo(target.CheckPath);
            DirectoryInfo sourceFolder = new DirectoryInfo($@"{BackupRoot}\{target.BackupFolder}");
            if (!sourceFolder.Exists) {
                target.Result = RestoreResult.NoBackupExists;
                return;
            }
            if (!destFolder.Exists)
                Directory.CreateDirectory(destFolder.FullName);
            KillProcessTree(target.ProcessName);
            try {
                CopyDir(sourceFolder.FullName, destFolder.FullName, true);
                target.Result = RestoreResult.RestoreComplete;
            } catch (IOException) {
                KillProcessTree(target.ProcessName);
                try {
                    CopyDir(sourceFolder.FullName, destFolder.FullName, true);
                    target.Result = RestoreResult.RestoreComplete;
                } catch (IOException) {
                    target.Result = RestoreResult.MergeFailed;
                    return;
                }
            }
            target.Result = RestoreResult.RestoreComplete;
        }
    }
}
