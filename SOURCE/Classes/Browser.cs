using System.IO;
using System.Text.Json.Serialization;

#nullable enable
namespace UserDataBackup.Classes {
    public class Browser {
        [JsonIgnore] public string? Bookmark_Base;
        public string? Bookmark_Path;
        public string? Backup_Path;
        public string? LiveBackup_Path;
        public FileInfo Live_Bookmark_File => new FileInfo(Bookmark_Path);
        public FileInfo Backup_Bookmark_File => new FileInfo(Backup_Path);
    }
}
#nullable disable