namespace UserDataBackup.Classes {
    public enum TargetType {
        Chrome,
        Edge,
        Firefox,
        StickyNotes,
        OutlookSigs,
        AsUType,
        Npp,
        AutoDest,
        Other = 99
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
