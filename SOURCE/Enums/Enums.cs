namespace UserDataBackup.Classes {
    public enum TargetApp {
        Chrome,
        Edge,
        Firefox,
        StickyNotes,
        OutlookSigs,
        AsUType,
        Npp,
        AutoDest,
        Other = 99,
        Comment = -1
    }
    public enum RestoreResult {
        NotAttempted,
        NoBackupExists,
        RestoreComplete,
        NoNewProfile,
        RestoreMerge,
        MergeFailed
    }
    public enum ClassType {
        Application,
        Browser,
        Unknown = 99
    }
}
