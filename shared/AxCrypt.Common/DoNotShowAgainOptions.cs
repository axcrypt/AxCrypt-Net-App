namespace AxCrypt.Common
{
    [Flags]
    public enum DoNotShowAgainOptions
    {
        None = 0x0000,
        FileAssociationBrokenWarning = 0x0001,
        LavasoftWebCompanionExistenceWarning = 0x0002,
        TryPremium = 0x0004,
        SignedInSoNoPasswordRequired = 0x0008,
        WillNotForgetPassword = 0x0010,
        IgnoreFileWarning = 0x0020,
        UnopenableFileWarning = 0x0040,
        KeySharingRemovedInFreeModeWarning = 0x0080,
        MasterKeyWarning = 0x0100,
        MasterKeyRemovedWarning = 0x0200,
        VaultDragDropWarning = 0x0400,
        FilePasswordWarning = 0x0800,
        UpgradeSubscriptionWarning = 0x1000,
    }
}
