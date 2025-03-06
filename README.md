# UserDataManagement

## Intent

UserDataManagement is intended to back up certain application specific data to OneDrive that is not normally captured. This includes bookmark/favorite data for Edge, Chrome, and Firefox; and profile data for StickyNotes, Outlook Signatures, AsUType, and Notepad++.

## JSON Configuration

UserDataManagement will look for a JSON file in its executing directory called BackupTargets.json

<details>

<summary>BackupTargets.json definitions</summary>

BackupTargets.json consists of an array of objects.  Each object is structured like this:

```JSON
  {
    "App": 3,
    "AppFile": "plum.sqlite",
    "AppPath": "{0}\\Packages\\Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe\\LocalState",
    "BackupFolder": "StickyNotes",
    "FriendlyName": "Sticky Notes",
    "ProcessName": [ "Microsoft.Notes" ],
    "RequireExisting": false,
    "Roaming": false,
    "TargetType": 0
  }
```

### JSON Fields
- App: `int`\\`enum` Maps to an internal enum. Custom targets should use 99.
- AppFile: `string?` The single file that should be targeted. Should be `null` if `TargetType` is 1
- AppPath: `string` The folder that should be targeted. If `TargetType` is 0, should be the parent folder of `AppFile`. Note: to keep JSON files user-agnostic, the strings associated with %appdata% or %localappdata% are replaced with {0}. See `Roaming`
- BackupFolder: `string` The name of the folder that will be created in the backup location
- FriendlyName: `string` The name used for this application in UI
- ProcessName: `string[]` An array of process names that should be terminated before beginning backup/restore operations on this application
- RequireExisting: `bool` Set `true` if restore operations should terminate if the target `AppFile`\\`AppPath` do not exist
- Roaming: `bool` Set true if {0} in `AppPath` should be interpolated with %appdata%. Otherwise, it will interpolate with %localappdata%
- TargetType: `int`\\`enum` Maps to an internal enum. 0 if the backup targets a single file, 1 if the backup targets a directory structure. Other numbers are not defined. If set to 0, `AppFile` must be a valid file. If set to 1, `AppFile` should be `null`

</details>  
  
## Unattended Usage

### silentback

UserDataManagement.exe silentback

This will silently conduct backup operations.  It will not wait for OD sync to complete.

### silentrest

UserDataManagement.exe silentrest

This will silently conduct restore operations. It may overwrite existing data without confirmation.