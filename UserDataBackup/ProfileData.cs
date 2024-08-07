using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using SapphTools.BookmarkManager.Chromium;
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
                },
                ["Notepad++"] = new BackupTarget {
                    CheckPath = new FileInfo($@"{appdata}\Notepad++\session.xml"),
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
        public void TryMigrate() {
            if (Directory.Exists($@"{Properties.Resources.OldBackup}\Chrome")) {
                if (!Directory.Exists($@"{BackupRoot}\Chrome"))
                    Directory.CreateDirectory($@"{BackupRoot}\Chrome");
                File.Copy(chrome.Live_Bookmark_File.FullName, chrome.Backup_Path, true);
            }
            if (Directory.Exists($@"{Properties.Resources.OldBackup}\Edge")) {
                if (!Directory.Exists($@"{BackupRoot}\Edge"))
                    Directory.CreateDirectory($@"{BackupRoot}\Edge");
                File.Copy(edge.Live_Bookmark_File.FullName, edge.Backup_Path, true);
            }
            if (Directory.Exists($@"{Properties.Resources.OldBackup}\Firefox")) {
                if (!Directory.Exists($@"{BackupRoot}\Firefox"))
                    Directory.CreateDirectory($@"{BackupRoot}\Firefox");
                CopyDir($@"{Properties.Resources.OldBackup}\Firefox", $@"{BackupRoot}\Firefox", null, false);
            }
            if (Directory.Exists($@"{Properties.Resources.OldBackup}\Sticky Notes")) {
                if (!Directory.Exists($@"{BackupRoot}\StickyNotes"))
                    Directory.CreateDirectory($@"{BackupRoot}\StickyNotes");
                CopyDir($@"{Properties.Resources.OldBackup}\Sticky Notes", $@"{BackupRoot}\StickyNotes", null, false);
            }
            if (Directory.Exists($@"{Properties.Resources.OldBackup}\npp")) {
                if (!Directory.Exists($@"{BackupRoot}\npp"))
                    Directory.CreateDirectory($@"{BackupRoot}\npp");
                CopyDir($@"{Properties.Resources.OldBackup}\npp", $@"{BackupRoot}\npp", null, false);
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
        private void KillProcessTree(string[] ProcessNames) {
            foreach (string ProcessName in ProcessNames) { 
                KillProcessTree(ProcessName); 
            }
        }
        public void Backup() {
            BackupChrome();
            BackupEdge();
            BackupFirefox();
            BackupStickyNotes();
            BackupSignatures();
            BackupAsUType();
            BackupNpp();
        }
        public bool Restore() {
            if (!Directory.Exists(BackupRoot) && !Directory.Exists(Properties.Resources.OldBackup))
                return false;
            if (!Directory.Exists(BackupRoot) && Directory.Exists(Properties.Resources.OldBackup))
                TryMigrate();
            RestoreChrome();
            RestoreFirefox();
            RestoreEdge();
            RestoreStickyNotes();
            RestoreSignatures();
            RestoreAsUType();
            RestoreNpp();
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
            KillProcessTree(target.ProcessName);
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
            KillProcessTree(firefox.ProcessName);
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
            KillProcessTree("Microsoft.Notes");
            ((FileInfo)thisTarget.CheckPath).CopyTo(stickyBackup.FullName, true);
        }
        private void BackupSignatures() {
            BackupTarget thisTarget = Targets["Outlook Signatures"];
            if (!thisTarget.TargetExists)
                return;
            DirectoryInfo sigBackup = new DirectoryInfo($@"{BackupRoot}\Signatures");
            if (!sigBackup.Exists)
                Directory.CreateDirectory(sigBackup.FullName);
            KillProcessTree("OUTLOOK");
            CopyDir(thisTarget.CheckPath.FullName, sigBackup.FullName, true);
        }
        private void BackupAsUType() {
            BackupTarget thisTarget = Targets["AsUType"];
            if (!thisTarget.TargetExists)
                return;
            if (!Directory.Exists($@"{BackupRoot}\AsUType"))
                Directory.CreateDirectory($@"{BackupRoot}\AsUType");
            KillProcessTree(new string[] { "AsutypePro32", "AsutypePro64" });
            string[] config = File.ReadAllLines($@"{localappdata}\Fanix\Asutype Professional\asutype.config", System.Text.Encoding.Unicode);
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
            File.WriteAllLines($@"{localappdata}\Fanix\Asutype Professional\asutype.config", config, System.Text.Encoding.Unicode);
            File.Copy($@"{localappdata}\Fanix\Asutype Professional\asutype.config", $@"{BackupRoot}\AsUType\asutype.config", true);
            CopyDir(@"C:\Users\Public\Documents\Fanix\Asutype Professional", $@"{BackupRoot}\AsUType", "*.correction", false);
            CopyDir(@"C:\Users\Public\Documents\Fanix\Asutype Professional", $@"{BackupRoot}\AsUType", "*.shortcut", false);
            CopyDir(@"C:\Users\Public\Documents\Fanix\Asutype Professional", $@"{BackupRoot}\AsUType", "*.spelling", false);
        }
        private void BackupNpp() {
            BackupTarget thisTarget = Targets["Notepad++"];
            if (!thisTarget.TargetExists)
                return;
            FileInfo nppBackup = new FileInfo($@"{BackupRoot}\npp\session.xml");
            string fileTarget = $@"{nppBackup.Directory.FullName}\files";
            if (!nppBackup.Directory.Exists) {
                nppBackup.Directory.Create();
                Directory.CreateDirectory(fileTarget);
            }
            KillProcessTree("notepad++");
            ((FileInfo)thisTarget.CheckPath).CopyTo(nppBackup.FullName, true);
            CopyDir($@"{appdata}\Notepad++\backup", fileTarget, false);
        }
        private void RestoreChromium(Browser target) {
            BackupTarget local = Targets[target.IndexName];
            if (!target.Backup_Bookmark_File.Exists) {
                local.RestoreResult = RestoreResult.NoBackupExists;
                RestoreResults[target.IndexName] = RestoreResult.NoBackupExists;
                Targets[target.IndexName] = local;
                return;
            }
            if (!target.Live_Bookmark_File.Directory.Exists) {
                local.RestoreResult = RestoreResult.NoNewProfile;
                RestoreResults[target.IndexName] = RestoreResult.NoNewProfile;
                Targets[target.IndexName] = local;
                return;
            }
            if (!target.Live_Bookmark_File.Exists) {
                KillProcessTree(target.ProcessName);
                try {
                    target.Backup_Bookmark_File.CopyTo(target.Bookmark_Path);
                } catch (IOException) {
                    KillProcessTree(target.ProcessName);
                    try {
                        target.Backup_Bookmark_File.CopyTo(target.Bookmark_Path);
                    } catch (IOException) {
                        local.RestoreResult = RestoreResult.MergeFailed;
                        RestoreResults[target.IndexName] = RestoreResult.MergeFailed;
                        Targets[target.IndexName] = local;
                        return;
                    }
                }
                local.RestoreResult = RestoreResult.RestoreComplete;
                RestoreResults[target.IndexName] = RestoreResult.RestoreComplete;
                Targets[target.IndexName] = local;
                return;
            }
            BookmarkFile live = new BookmarkFile(target.Bookmark_Path);
            BookmarkFile backup = new BookmarkFile(target.Backup_Path);
            KillProcessTree(target.ProcessName);
            try {
                if (!live.Merge(backup, out BookmarkFile merge)) {
                    RestoreResults[target.IndexName] = RestoreResult.MergeFailed;
                    return;
                }
                try {
                    merge.WriteFile(live.FilePath);
                    FileInfo live_bookmarkbackup = new FileInfo(target.LiveBackup_Path);
                    live_bookmarkbackup.Delete();
                } catch (IOException) {
                    KillProcessTree(target.ProcessName);
                    try {
                        merge.WriteFile(live.FilePath);
                        FileInfo live_bookmarkbackup = new FileInfo(target.LiveBackup_Path);
                        live_bookmarkbackup.Delete();
                    } catch (IOException) {
                        local.RestoreResult = RestoreResult.MergeFailed;
                        RestoreResults[target.IndexName] = RestoreResult.MergeFailed;
                        Targets[target.IndexName] = local;
                        return;
                    }
                }
            } catch {
                local.RestoreResult = RestoreResult.MergeFailed;
                RestoreResults[target.IndexName] = RestoreResult.MergeFailed;
                Targets[target.IndexName] = local;
                return;
            }
            local.RestoreResult = RestoreResult.RestoreComplete;
            RestoreResults[target.IndexName] = RestoreResult.RestoreComplete;
            Targets[target.IndexName] = local;
            return;
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
            KillProcessTree(firefox.ProcessName);
            try {
                CopyDir(firefox.Backup_Path, firefox.Bookmark_Path, true);
            } catch (IOException) {
                KillProcessTree(firefox.ProcessName);
                try {
                    CopyDir(firefox.Backup_Path, firefox.Bookmark_Path, true);
                } catch (IOException) {
                    local.RestoreResult = RestoreResult.MergeFailed;
                    RestoreResults[firefox.IndexName] = RestoreResult.MergeFailed;
                    Targets[firefox.IndexName] = local;
                    return;
                }
            }

            local.RestoreResult = RestoreResult.RestoreComplete;
            RestoreResults[firefox.IndexName] = RestoreResult.RestoreComplete;
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
            stickyBackup.CopyTo(stickyfile.FullName, true);
            KillProcessTree("Microsoft.Notes");
            try {
                stickyBackup.CopyTo(stickyfile.FullName, true);
            } catch (IOException) {
                KillProcessTree("Microsoft.Notes");
                try {
                    stickyBackup.CopyTo(stickyfile.FullName, true);
                } catch (IOException) {
                    local.RestoreResult = RestoreResult.MergeFailed;
                    RestoreResults["StickyNotes"] = RestoreResult.MergeFailed;
                    Targets["StickyNotes"] = local;
                    return;
                }
            }
            local.RestoreResult = RestoreResult.RestoreComplete;
            RestoreResults["StickyNotes"] = RestoreResult.RestoreComplete;
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
            KillProcessTree("OUTLOOK");
            try {
                CopyDir(sigBackup.FullName, sigFolder.FullName, true);
                local.RestoreResult = RestoreResult.RestoreComplete;
            } catch (IOException) {
                KillProcessTree("OUTLOOK");
                try {
                    CopyDir(sigBackup.FullName, sigFolder.FullName, true);
                    local.RestoreResult = RestoreResult.RestoreComplete;
                } catch (IOException) {
                    RestoreResults["Outlook Signatures"] = RestoreResult.MergeFailed;
                    local.RestoreResult = RestoreResult.MergeFailed;
                    Targets["Outlook Signatures"] = local;
                    return;
                }
            }
            local.RestoreResult = RestoreResult.RestoreComplete;
            Targets["Outlook Signatures"] = local;
            RestoreResults["Outlook Signatures"] = RestoreResult.RestoreComplete;
        }
        private void RestoreAsUType() {
            BackupTarget local = Targets["AsUType"];
            if (!File.Exists($@"{BackupRoot}\AsUType\asutype.config")) {
                local.RestoreResult = RestoreResult.NoBackupExists;
                Targets["AsUType"] = local;
                return;
            }
            if (!Directory.Exists($@"{localappdata}\Fanix\Asutype Professional\"))
                Directory.CreateDirectory($@"{localappdata}\Fanix\Asutype Professional\");
            KillProcessTree(new string[] { "AsutypePro32", "AsutypePro64" });
            try {
                CopyDir($@"{BackupRoot}\AsUType", @"C:\Users\Public\Documents\Fanix\Asutype Professional", "*.correction", false);
                CopyDir($@"{BackupRoot}\AsUType", @"C:\Users\Public\Documents\Fanix\Asutype Professional", "*.shortcut", false);
                CopyDir($@"{BackupRoot}\AsUType", @"C:\Users\Public\Documents\Fanix\Asutype Professional", "*.spelling", false);
                File.Copy($@"{BackupRoot}\AsUType\asutype.config", $@"{localappdata}\Fanix\Asutype Professional\asutype.config", true);
            } catch (IOException) {
                KillProcessTree(new string[] { "AsutypePro32", "AsutypePro64" });
                try {
                    CopyDir($@"{BackupRoot}\AsUType", @"C:\Users\Public\Documents\Fanix\Asutype Professional", "*.correction", false);
                    CopyDir($@"{BackupRoot}\AsUType", @"C:\Users\Public\Documents\Fanix\Asutype Professional", "*.shortcut", false);
                    CopyDir($@"{BackupRoot}\AsUType", @"C:\Users\Public\Documents\Fanix\Asutype Professional", "*.spelling", false);
                    File.Copy($@"{BackupRoot}\AsUType\asutype.config", $@"{localappdata}\Fanix\Asutype Professional\asutype.config", true);
                } catch {
                    local.RestoreResult = RestoreResult.MergeFailed;
                    RestoreResults["AsUType"] = RestoreResult.MergeFailed;
                    Targets["AsUType"] = local;
                    return;
                }
            }
            local.RestoreResult = RestoreResult.RestoreComplete;
            RestoreResults["AsUType"] = RestoreResult.RestoreComplete;
            Targets["AsUType"] = local;
        }
        private void RestoreNpp() {
            BackupTarget local = Targets["Notepad++"];
            FileInfo nppFile = (FileInfo)local.CheckPath;
            FileInfo nppBackupFile = new FileInfo($@"{BackupRoot}\npp\session.xml");
            if (!nppBackupFile.Exists) {
                local.RestoreResult = RestoreResult.NoBackupExists;
                Targets["Notepad++"] = local;
                return;
            }
            if (!Directory.Exists(nppFile.Directory.FullName)) {
                nppFile.Directory.Create();
            }
            KillProcessTree("notepad++"); //FINISH TRYCATCH
            try {
                nppBackupFile.CopyTo(nppFile.FullName, true);
                DirectoryInfo nppBackupFolder = new DirectoryInfo($@"{nppFile.Directory.FullName}\backup");
                if (!nppBackupFolder.Exists)
                    Directory.CreateDirectory(nppBackupFolder.FullName);
                CopyDir($@"{nppBackupFile.DirectoryName}\files", nppBackupFolder.FullName, false);
            } catch (IOException) {
                KillProcessTree("notepad++");
                try {
                    nppBackupFile.CopyTo(nppFile.FullName, true);
                    DirectoryInfo nppBackupFolder = new DirectoryInfo($@"{nppFile.Directory.FullName}\backup");
                    if (!nppBackupFolder.Exists)
                        Directory.CreateDirectory(nppBackupFolder.FullName);
                    CopyDir($@"{nppBackupFile.DirectoryName}\files", nppBackupFolder.FullName, false);
                } catch (IOException) {
                    local.RestoreResult = RestoreResult.MergeFailed;
                    RestoreResults["Notepad++"] = RestoreResult.MergeFailed;
                    Targets["Notepad++"] = local;
                    return;
                }
            }

            local.RestoreResult = RestoreResult.RestoreComplete;
            RestoreResults["Notepad++"] = RestoreResult.RestoreComplete;
            Targets["Notepad++"] = local;
        }
    }
}
