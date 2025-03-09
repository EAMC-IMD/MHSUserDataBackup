using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using SapphTools.BookmarkManager.Chromium;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Text.Json;
using System.Text;
using System.Security;
using UserDataBackup.Enums;

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
            jsonText ??= Encoding.UTF8.GetString(Properties.Resources.BackupTargets);
            BackupTargets = JsonSerializer.Deserialize<TargetCollection>(jsonText)!;
        }
        private static void CopyData(string sourcePath, string destinationFolder, FileSystemType type) {
            CopyData(sourcePath, destinationFolder, type, true);
        }
        private static void CopyData(string sourcePath, string destinationFolder, FileSystemType type, bool recursive) {
            CopyData(sourcePath, destinationFolder, type, null, null, recursive);
        }
        private static void CopyData(string sourcePath, string destinationFolder, FileSystemType type, string? filter, bool recursive) {
            CopyData(sourcePath, destinationFolder, type, filter, null, recursive);
        }
        private static void CopyData(string sourcePath, string destinationFolder, FileSystemType type, string? filter, string? exclusionFilter, bool recursive) {
            if (type == FileSystemType.Unknown)
                return;
            if (type == FileSystemType.File) {
                FileInfo source = new FileInfo(sourcePath);
                if (!source.Exists)
                    return;
                if (File.Exists(destinationFolder))
                    destinationFolder = (new FileInfo(destinationFolder)).DirectoryName;
                if (!Directory.Exists(destinationFolder))
                    Directory.CreateDirectory(destinationFolder);
                string sourceFileName = source.Name;
                string destinationFile = Path.Combine(destinationFolder, sourceFileName);
                source.CopyTo(destinationFile, true);
                return;
            }
            if (type == FileSystemType.Directory) {
                if (!Directory.Exists(sourcePath))
                    return;
                if (!Directory.Exists(destinationFolder))
                    Directory.CreateDirectory(destinationFolder);
                CopyDir(sourcePath, destinationFolder, filter, exclusionFilter, recursive);
            }
        }
        private static void CopyDir(string sourcePath, string destinationPath, bool recursive) {
            CopyDir(sourcePath, destinationPath, null, recursive);
        }
        private static void CopyDir(string sourcePath, string destinationPath, string? filter, bool recursive) {
            CopyDir(sourcePath, destinationPath, filter, null, recursive);
        }
        private static void CopyDir(string sourcePath, string destinationPath, string? filter, string? exclusionFilter, bool recursive) {
            DirectoryInfo sourceInfo = new DirectoryInfo(sourcePath);
            if (Regex.IsMatch(sourceInfo.FullName, @"Firefox\\Profiles\\.*\\storage\\default"))
                return;
            if (!sourceInfo.Exists)
                throw new DirectoryNotFoundException();
            DirectoryInfo[] subSource = sourceInfo.GetDirectories();
            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);
            foreach (FileInfo file in filter is null ? sourceInfo.GetFiles() : sourceInfo.GetFiles(filter)) {
                if (exclusionFilter != null && Regex.IsMatch(file.Name, exclusionFilter))
                    continue;
                string filePath = Path.Combine(destinationPath, file.Name);
                file.CopyTo(filePath, true);
            }
            if (recursive) {
                foreach (DirectoryInfo subDir in subSource) {
                    string newTarget = Path.Combine(destinationPath, subDir.Name);
                    CopyDir(subDir.FullName, newTarget, true);
                }
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
        private bool TargetExists(string path, FileSystemType type) {
            if (type == FileSystemType.File)
                return File.Exists(path);
            if (type == FileSystemType.Directory)
                return Directory.Exists(path);
            return false;
        }
        private string GetFullBackupPath(string path) => $@"{BackupRoot}\{path}";
        public void Backup() {
            foreach (BackupTarget target in BackupTargets) {
                if (!target.Valid)
                    continue;
                switch (target.App) {
                    case TargetApp.Comment:
                        continue;
                    case TargetApp.Chrome:
                    case TargetApp.Edge:
                    case TargetApp.StickyNotes:
                    case TargetApp.OutlookSigs:
                    case TargetApp.AutoDest:
                    case TargetApp.Other:
                    default:
                        GenericBackup(target);
                        break;
                    case TargetApp.Firefox:
                        BackupFirefox(target);
                        break;
                    case TargetApp.AsUType:
                        BackupAsUType(target);
                        break;
                    case TargetApp.Npp:
                        BackupNpp(target);
                        break;
                }
            }
        }
        public bool Restore() {
            if (!Directory.Exists(BackupRoot))
                return false;
            foreach (BackupTarget target in BackupTargets) {
                if (!target.Valid)
                    continue;
                switch (target.App) {
                    case TargetApp.Comment:
                        continue;
                    case TargetApp.Chrome:
                    case TargetApp.Edge:
                        ChromiumRestore(target);
                        break;
                    case TargetApp.Firefox:
                        RestoreFirefox(target);
                        break;
                    case TargetApp.AsUType:
                        RestoreAsUType(target);
                        break;
                    case TargetApp.Npp:
                        RestoreNpp(target);
                        break;
                    case TargetApp.StickyNotes:
                    case TargetApp.OutlookSigs:
                    case TargetApp.AutoDest:
                    case TargetApp.Other:
                    default:
                        GenericRestore(target);
                        break;
                }
            }
            int failCount = BackupTargets.Where(bt => bt.App != TargetApp.Comment).Where(bt => bt.Result == RestoreResult.NoNewProfile).Count();
            failCount += BackupTargets.Where(bt => bt.App != TargetApp.Comment).Where(bt => bt.Result == RestoreResult.MergeFailed).Count();
            if (failCount > 0)
                return false;
            return true;
        }
        private void GenericBackup(BackupTarget target) {
            string appFullPath = target.AppFile is null ? target.AppPath : Path.Combine(target.AppPath, target.AppFile);
            if (!TargetExists(appFullPath, target.TargetType))
                return;
            string backupFolder = GetFullBackupPath(target.BackupFolder);
            if (!Directory.Exists(backupFolder))
                Directory.CreateDirectory(backupFolder);
            KillProcessTree(target.ProcessName);
            try {
                CopyData(appFullPath, backupFolder, FileSystemType.File);
            } catch (ArgumentNullException) {
                Debug.WriteLine("Unreachable.");
            } catch (Exception e) when (e is SecurityException || e is UnauthorizedAccessException) {
                Debug.WriteLine($"Access denied to {appFullPath} or {backupFolder}");
            } catch (Exception e) when (e is ArgumentException || e is PathTooLongException || e is NotSupportedException) {
                Debug.WriteLine($"Path contained invalid data : {appFullPath} : {backupFolder}");
            }
        }
        private void BackupFirefox(BackupTarget target) {
            if (!TargetExists(target.AppPath, target.TargetType))
                return;
            string backupFolder = GetFullBackupPath(target.BackupFolder);
            if (!Directory.Exists(target.BackupFolder))
                Directory.CreateDirectory(target.BackupFolder);
            KillProcessTree(target.ProcessName);
            try {
                CopyData(target.AppPath, backupFolder, FileSystemType.Directory, null, @"\.com$", true);
            } catch (ArgumentNullException) {
                Debug.WriteLine("Unreachable.");
            } catch (Exception e) when (e is SecurityException || e is UnauthorizedAccessException) {
                Debug.WriteLine($"Access denied to {target.AppPath} or {backupFolder}");
            } catch (Exception e) when (e is ArgumentException || e is PathTooLongException || e is NotSupportedException) {
                Debug.WriteLine($"Path contained invalid data : {target.AppPath} : {backupFolder}");
            }
        }
        private void BackupAsUType(BackupTarget target) {
            if (!TargetExists(target.AppPath, target.TargetType))
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
        private void BackupNpp(BackupTarget target) {
            GenericBackup(target);
            string extraFolder = Path.Combine(GetFullBackupPath(target.BackupFolder), "backup");
            CopyDir($@"{_appdata}\Notepad++\backup", extraFolder, false);
        }
        private void ChromiumRestore(BackupTarget target) {
            string backupFullPath = target.AppFile is null ? GetFullBackupPath(target.BackupFolder) : Path.Combine(GetFullBackupPath(target.BackupFolder), target.AppFile);
            string appFullPath = target.AppFile is null ? target.AppPath : Path.Combine(target.AppPath, target.AppFile);
            if (!TargetExists(backupFullPath, target.TargetType)) {
                target.Result = RestoreResult.NoBackupExists;
                return;
            }
            if (!TargetExists(target.AppPath, FileSystemType.Directory)) { 
                target.Result = RestoreResult.NoNewProfile;
                return;
            }
            if (!TargetExists(appFullPath, target.TargetType)) {
                KillProcessTree(target.ProcessName);
                try {
                    CopyData(backupFullPath, target.AppPath, target.TargetType);
                } catch (IOException) {
                    KillProcessTree(target.ProcessName);
                    try {
                        CopyData(backupFullPath, target.AppPath, target.TargetType);
                    } catch (Exception) {
                        target.Result = RestoreResult.MergeFailed;
                        return;
                    }
                }
                target.Result = RestoreResult.RestoreComplete;
                return;
            }
            string liveJson, backupJson;
            try {
                liveJson = File.ReadAllText(appFullPath);
                backupJson = File.ReadAllText(backupFullPath);
            } catch {
                target.Result = RestoreResult.MergeFailed;
                return;
            }
            if (BookmarkFile.Deserialize(liveJson, out BookmarkFile? live) || live is null) {
                target.Result = RestoreResult.MergeFailed;
                return;
            }
            if (BookmarkFile.Deserialize(backupFullPath, out BookmarkFile? backup) || backup is null) {
                target.Result = RestoreResult.MergeFailed;
                return;
            }
            KillProcessTree(target.ProcessName);
            try {
                live.Merge(backup);
                try {
                    string json = live.Serialize();
                    File.WriteAllText(appFullPath, json);
                } catch (IOException) {
                    KillProcessTree(target.ProcessName);
                    try {
                        string json = live.Serialize();
                        File.WriteAllText(appFullPath, json);
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
        private void RestoreFirefox(BackupTarget target) {
            string backupFullPath = target.AppFile is null ? GetFullBackupPath(target.BackupFolder) : Path.Combine(GetFullBackupPath(target.BackupFolder), target.AppFile);
            if (!TargetExists(backupFullPath, target.TargetType)) {
                target.Result = RestoreResult.NoBackupExists;
                return;
            }

            KillProcessTree(target.ProcessName);
            try {
                CopyData(backupFullPath, target.AppPath, target.TargetType);
            } catch (IOException) {
                KillProcessTree(target.ProcessName);
                try {
                    CopyData(backupFullPath, target.AppPath, target.TargetType);
                } catch (Exception) {
                    target.Result = RestoreResult.MergeFailed;
                    return;
                }
            }
            target.Result = RestoreResult.RestoreComplete;
            return;

        }
        private void GenericRestore(BackupTarget target) {
            string backupFullPath = target.AppFile is null ? GetFullBackupPath(target.BackupFolder) : Path.Combine(GetFullBackupPath(target.BackupFolder), target.AppFile);
            string appFullPath = target.AppFile is null ? target.AppPath : Path.Combine(target.AppPath, target.AppFile);
            if (!TargetExists(backupFullPath, target.TargetType)) {
                target.Result = RestoreResult.NoBackupExists;
                return;
            }
            if (!TargetExists(target.AppPath, FileSystemType.Directory)) {
                if (target.RequireExisting) {
                    target.Result = RestoreResult.NoNewProfile;
                    return;
                } else
                    Directory.CreateDirectory(target.AppPath);
            }
            KillProcessTree(target.ProcessName);
            try {
                CopyData(backupFullPath, appFullPath, target.TargetType);
            } catch (IOException) {
                KillProcessTree(target.ProcessName);
                try {
                    CopyData(backupFullPath, appFullPath, target.TargetType);
                } catch (Exception) {
                    target.Result = RestoreResult.MergeFailed;
                    return;
                }
            }
            target.Result = RestoreResult.RestoreComplete;
            return;
        }
        private void RestoreAsUType(BackupTarget target) {
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
        private void RestoreNpp(BackupTarget target) {
            string backupFullPath = target.AppFile is null ? GetFullBackupPath(target.BackupFolder) : Path.Combine(GetFullBackupPath(target.BackupFolder), target.AppFile);
            if (!TargetExists(backupFullPath, target.TargetType)) {
                target.Result = RestoreResult.NoBackupExists;
                return;
            }

            if (!Directory.Exists(target.AppPath)) {
                Directory.CreateDirectory(target.AppPath);
            }
            KillProcessTree(target.ProcessName); 
            try {
                CopyData(backupFullPath, target.AppPath, FileSystemType.Directory);
            } catch (IOException) {
                KillProcessTree(target.ProcessName);
                try {
                    CopyData(backupFullPath, target.AppPath, FileSystemType.Directory);
                } catch (IOException) {
                    target.Result = RestoreResult.MergeFailed;
                    return;
                }
            }

            target.Result = RestoreResult.RestoreComplete;
        }
    }
}
