using System.IO;
using UserDataBackup.Enums;

#nullable enable
namespace UserDataBackup.Classes {
    internal static class FileSystemHelper {
        internal static FileSystemType GetFSIType(string? path) {
            if (Directory.Exists(path)) {
                return FileSystemType.Directory;
            }
            if (File.Exists(path)) {
                return FileSystemType.File;
            }
            return FileSystemType.Unknown;
        }
    }
}
#nullable disable