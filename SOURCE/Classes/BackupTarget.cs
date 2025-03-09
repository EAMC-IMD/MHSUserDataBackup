using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using UserDataBackup.Enums;

#nullable enable
namespace UserDataBackup.Classes {
    public class BackupTarget : IEquatable<BackupTarget> {
        #region Fields
        protected string _unformattedAppPath = "";
        #endregion
        #region Const and Readonly
        protected static readonly string _localappdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        protected static readonly string _appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        #endregion
        #region Properties
        [JsonInclude] public TargetApp App { get; set; }

        [JsonInclude]
        public string? AppFile { get; set; }
        [JsonPropertyName("AppPath")]
        [JsonInclude]
        public string UnformattedAppPath {
            get => _unformattedAppPath;
            set {
                _unformattedAppPath = value;
                TargetType = FileSystemHelper.GetFSIType(AppPath);
            }
        }
        [JsonInclude] public string BackupFolder { get; set; } = "";
        [JsonInclude] public string FriendlyName { get; set; } = "";
        [JsonInclude] public List<string> ProcessName { get; set; } = new List<string>();
        [JsonInclude] public bool RequireExisting { get; set; } = true;
        [JsonInclude] public bool Roaming { get; set; }
        [JsonInclude] public FileSystemType TargetType { get; set; }
        [JsonIgnore]  public RestoreResult Result { get; internal set; } = RestoreResult.NotAttempted;

        [JsonIgnore]
        public string AppPath {
            get {
                return string.Format(_unformattedAppPath, Roaming ? _appdata : _localappdata);
            }
            set {
                if (value.Contains(_localappdata)) {
                    Roaming = false;
                    _unformattedAppPath = value.Replace(_localappdata, @"{0}");
                } else {
                    Roaming = true;
                    _unformattedAppPath = value.Replace(_appdata, @"{0}");
                }
                TargetType = FileSystemHelper.GetFSIType(value);
            }
        }

        [JsonIgnore]
        public bool Valid => (AppPath != "" && BackupFolder != "" && FriendlyName != "") && ((TargetType == FileSystemType.File && !string.IsNullOrWhiteSpace(AppFile)) ||
        (TargetType == FileSystemType.Directory));
        #endregion
        #region ctor
        #endregion
        #region IEquitable Implementation
        public override bool Equals(object obj) => Equals(obj as BackupTarget);
        public override int GetHashCode() {
            return App.GetHashCode();
        }
        public bool Equals(BackupTarget? other) {
            if (other is null)
                return false;
            return App.Equals(other.App);
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
