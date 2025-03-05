namespace UserDataBackup.Classes {
    public class ApplicationTarget : BackupTarget {
        public new ClassType ObjectClass { get; } = ClassType.Application;
    }
}
