using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text.Json.Serialization;

#nullable enable
namespace UserDataBackup.Classes {
    public class BackupTarget : IEquatable<BackupTarget> {
        #region Fields
        private FileInfo? _checkPathInfo;
        private string _checkPath = "";
        #endregion
        #region Const and Readonly
        private static readonly string _localappdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly string _appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        #endregion
        #region Properties
        public ClassType ObjectClass { get; set; }
        public TargetType Type { get; set; }
        public bool Roaming { get; set; }
        public string BackupFolder { get; set; } = "";
        public string FriendlyName { get; set; } = "";
        public List<string> ProcessName { get; set; } = new List<string>();
        [JsonIgnore] public RestoreResult Result { get; internal set; } = RestoreResult.NotAttempted;
        public string CheckPath {
            get => _checkPath;
            set {
                _checkPath = value;
                TargetExists = ValidateCheckPath(CheckPath, out _checkPathInfo) && _checkPathInfo != null && _checkPathInfo.Exists;
            }
        }
        [JsonIgnore] public FileInfo? CheckPathInfo => _checkPathInfo;
        [JsonIgnore] public bool TargetExists { get; internal set; } = false;
        #endregion
        #region ctor
        #endregion
        #region Methods
        private bool ValidateCheckPath(string? path, out FileInfo? info) {
            info = null;
            try {
                info = new FileInfo(path);
                return true;
            } catch (ArgumentNullException) {
                Debug.WriteLine("path was null");
            } catch (Exception e) when (e is SecurityException || e is UnauthorizedAccessException) {
                Debug.WriteLine($"Access denied to {path}");
            } catch (Exception e) when (e is ArgumentException || e is PathTooLongException || e is NotSupportedException) {
                Debug.WriteLine($"path contained invalid data : {path}");
            }
            return false;
        }
        public void Validate() {
            TargetExists = ValidateCheckPath(CheckPath, out _checkPathInfo) && _checkPathInfo != null && _checkPathInfo.Exists;
        }
        #endregion
        #region IEquitable Implementation
        public override bool Equals(object obj) => Equals(obj as BackupTarget);
        public override int GetHashCode() {
            return Type.GetHashCode();
        }
        public bool Equals(BackupTarget? other) {
            if (other is null)
                return false;
            return Type.Equals(other.Type);
        }
        public static bool operator ==(BackupTarget? target1, BackupTarget? target2) {
            if (ReferenceEquals(target1, target2))
                return true;
            if (target1 is null && target2 is null)
                return true;
            if (target1 is null || target2 is null)
                return false;
            return target1.Equals(target2);
        }
        public static bool operator !=(BackupTarget? target1, BackupTarget target2) => !(target1 == target2);
        #endregion
    }
}
#nullable disable
