using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

#nullable enable
namespace UserDataBackup.Classes {
    internal class TargetCollection : HashSet<BackupTarget> {
        [JsonIgnore] public Dictionary<string, RestoreResult> RestoreResults => this.ToDictionary(bt => bt.FriendlyName, bt => bt.Result);
        public BackupTarget? GetTarget(TargetType type) => this.Where(bt => bt.Type==type).FirstOrDefault();
    }
}
#nullable disable