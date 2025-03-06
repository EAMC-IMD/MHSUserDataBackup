using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

#nullable enable
namespace UserDataBackup.Classes {
    internal class TargetCollection : HashSet<BackupTarget> {
        [JsonIgnore] public Dictionary<string, RestoreResult> RestoreResults => this.Where(bt => bt.App != TargetApp.Comment).ToDictionary(bt => bt.FriendlyName, bt => bt.Result);
    }
}
#nullable disable