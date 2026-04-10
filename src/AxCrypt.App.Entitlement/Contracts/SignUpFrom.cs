namespace AxCrypt.App.Entitlement.Contracts;

/// <summary>
/// The plan tier a user originally signed up on. Used to tailor
/// upgrade prompts and the /upgradePage plan list so we never
/// pitch a tier the user already has (or already downgraded from).
///
///   None     → unknown / unauthenticated
///   Free     → signed up on the free tier
///   Premium  → signed up directly on Premium (we won't offer Free / Business
///              cross-sells; we only re-offer Premium when their plan lapses)
///   Business → signed up directly on Business (we only re-offer Business)
///
/// Populated from the user account data at sign-in (see
/// <c>AccountModel.SignUpFrom</c>). Distinct from <c>SubscriptionLevel</c>,
/// which reflects the user's *current* plan; SignUpFrom is sticky and
/// never changes after the first sign-in.
///
/// Lives in <c>AxCrypt.App.Entitlement.Contracts</c> (NOT in
/// AxCrypt.App.Shared) because that's the one-way reference direction:
/// Shared → Entitlement.
/// </summary>
public enum SignUpFrom
{
    None = 0,
    Free = 1,
    Premium = 2,
    Business = 3,
}
