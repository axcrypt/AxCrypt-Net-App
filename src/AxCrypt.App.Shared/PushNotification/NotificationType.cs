namespace AxCrypt.App.Shared.PushNotification;

public enum NotificationType
{
    None = 0,
    GetStarted,
    InviteUser,
    PasswordResetInitiated,
    PasswordReset,
    UserEmailChangeInitiated,
    UserEmailChanged,
    PaymentSuccessful,
    PaymentFailed,
    PaymentCancelled,
    SubscriptionExpiresSoon,
    SubscriptionExpired,
    SubscriptionCancelled,
    PaymentFailure,
    KeyShared,
    MasterKeyShared,
    ShareSecret,
    SecuredMessageSent
}
