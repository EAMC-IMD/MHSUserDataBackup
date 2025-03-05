using System.IO;
using System.Text.Json.Serialization;

#nullable enable
namespace UserDataBackup.Classes {
    public class BrowserTarget : BackupTarget {
        #region Fields
        private FileInfo? _checkPathInfo;
        private string _checkPath = "";
        #endregion
        #region Property Hiders
        [JsonIgnore]
        public new ClassType ObjectClass { get; } = ClassType.Browser;
        public new string CheckPath {
            get {
                if (BrowserInfo is null)
                    return _checkPath;
                return BrowserInfo.Bookmark_Path ?? _checkPath;
            }
            set {
                _checkPath = value;
                _checkPathInfo = new FileInfo(CheckPath);
            }
        }
        #endregion
        #region Properties
        public Browser? BrowserInfo { get; set; } = null;
        #endregion
    }
}
#nullable disable
