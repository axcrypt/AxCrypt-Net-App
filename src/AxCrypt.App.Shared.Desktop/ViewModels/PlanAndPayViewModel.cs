using AxCrypt.Api.Model;
using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.App.Shared.Desktop.Services;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels
{
    // Which plan is currently selected on the plan-and-pay page. Null selection (on PlanAndPayViewModel.SelectedPlan) means "not chosen yet".
    public enum PlanMode { Free, Premium, Business }

    // Owns all state and business logic for the Plan & Pay page; the razor view only renders it and forwards user gestures to the commands below.
    public class PlanAndPayViewModel : ViewModelBase
    {
        // Poll cadence while waiting for a checkout payment to be confirmed.
        private const int PollIntervalMs = 5_000;

        // Give up waiting for payment confirmation after this many polls (~15 minutes).
        private const int MaxPollAttempts = 180;

        // How long the success overlay stays visible before the caller navigates away.
        private const int PostSuccessDelayMs = 2_500;

        // How long to wait for rapid +/- member clicks to settle before re-pricing.
        private const int MemberDebounceMs = 400;

        // How long a pending checkout/activation can sit unresolved before we offer a "Log in" fallback.
        private const int LoginFallbackDelayMs = 15_000;

        private static readonly List<(string Code, string Name)> _countryList = BuildCountryList();

        private readonly RegisterViewModel _registerViewModel;
        private readonly LogOnViewModel _logOnViewModel;
        private readonly UserService _userService;
        private readonly OnlineSignUpService _signUp;
        private readonly SignUpViewModel _signUpVm;
        private readonly ProfileViewModel _profileViewModel;

        private IEnumerable<PricingInfoApiModel>? _pricingModels;

        // Undiscounted pricing snapshot taken right before a successful discount apply, so we can show the renewal price without a second API call.
        private IEnumerable<PricingInfoApiModel>? _pricingModelsBeforeDiscount;

        private CancellationTokenSource? _pollCts;
        private CancellationTokenSource? _memberDebounceCts;
        private CancellationTokenSource? _loginFallbackCts;
        private SubscriptionLevel _preCheckoutLevel = SubscriptionLevel.Unknown;
        private bool _isFreeActivationFlow;

        public PlanAndPayViewModel(UserService userService, OnlineSignUpService signUp, SignUpViewModel signUpVm, ProfileViewModel profileViewModel)
        {
            _registerViewModel = AxCServiceProviderExtension.RegisterViewModel!;
            _logOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
            _userService = userService;
            _signUp = signUp;
            _signUpVm = signUpVm;
            _profileViewModel = profileViewModel;
        }

        // Raised when a payment/activation flow finishes successfully and the caller should navigate home; kept separate from OnUpdateViewState so the razor doesn't have to inspect state to know when.
        public event Action? RequestNavigateHome;

        // ── Origin / eligibility ───────────────────────────────────────────
        // The signup path the user arrived from; Premium/Business origins are pinned to that plan.
        public SignUpFrom Origin => _userService.SignUpFrom;
        private bool IsBusinessOrigin => Origin == SignUpFrom.Business;

        // True when Free is a valid alternative to Premium: driven by signup eligibility for Premium origin, tied to the Private/Business toggle for None/Free origin, and never offered for pinned Business origin.
        public bool FreePlanAvailable => Origin switch
        {
            SignUpFrom.Premium => _signUp.EFF,
            SignUpFrom.None => !ShowingBusiness,
            SignUpFrom.Free => !ShowingBusiness,
            _ => false
        };

        // The reqFrom value actually sent to the pricing API — never None/Free, since the API doesn't recognise those as a plan; falls back to whichever plan is actually being priced.
        private SignUpFrom PricingRequestOrigin =>
            Origin == SignUpFrom.Premium || Origin == SignUpFrom.Business
                ? Origin
                : (ShowingBusiness ? SignUpFrom.Business : SignUpFrom.Premium);

        // True when this page was reached from a brand-new signup rather than an existing-user upgrade.
        public bool IsFromSignup => _signUpVm.IsFromSignup;

        // ── Plan selection ───────────────────────────────────────────────
        // Null = no plan picked yet (comparison view); set once the user chooses a card.
        public PlanMode? SelectedPlan { get; private set; }

        public bool ShowingBusiness => IsBusinessOrigin || SelectedPlan == PlanMode.Business;
        public bool ShowingFreeSolo => !ShowingBusiness && SelectedPlan == PlanMode.Free;
        public bool ShowingPremiumSolo => !ShowingBusiness && (SelectedPlan == PlanMode.Premium || !FreePlanAvailable);
        public bool ShowingComparison => !ShowingBusiness && FreePlanAvailable && SelectedPlan == null;
        public bool ShowingSoloIndividualPlan => ShowingFreeSolo || ShowingPremiumSolo;

        private SubscriptionLevel PricingLevel => ShowingBusiness ? SubscriptionLevel.Business : SubscriptionLevel.Premium;

        public void SelectFree()
        {
            SelectedPlan = PlanMode.Free;
            ShowCompanyValidationErrors = false;
        }

        public void SelectPremium()
        {
            SelectedPlan = PlanMode.Premium;
            ShowCompanyValidationErrors = false;
        }

        public void SelectBusinessPlan()
        {
            SelectedPlan = PlanMode.Business;
            ShowCompanyValidationErrors = false;
        }

        // Returns to the Free/Premium comparison (or Premium-solo, if Free isn't available).
        public void ShowPlanComparison()
        {
            SelectedPlan = null;
            ShowCompanyValidationErrors = false;
        }

        public bool YearlyBilling { get; private set; } = true;

        public void SetYearlyBilling(bool yearly) => YearlyBilling = yearly;

        // ── Business accounts available to this user, sourced from the latest pricing response ──
        public IEnumerable<KeyValuePair<string, string>> BusinessAccounts =>
            _pricingModels?.FirstOrDefault()?.BusinessInfo
            ?? Enumerable.Empty<KeyValuePair<string, string>>();

        public List<KeyValuePair<string, string>> Businesses { get; private set; } = new();
        public string SelectedBusinessId { get; private set; } = string.Empty;

        // ── Business company & billing form fields (bound directly from the view) ──
        public string OrgName { get; set; } = string.Empty;
        public string OrgCountry { get; set; } = string.Empty;
        public string OrgVat { get; set; } = string.Empty;
        public string InvoiceEmail { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public bool CompanyInfoFromSignup { get; private set; }
        public bool CompanyInfoExpanded { get; set; }

        public void ToggleCompanyInfoExpanded() => CompanyInfoExpanded = !CompanyInfoExpanded;

        // Billing Information stays expanded at all times per spec — no collapse state to manage.

        // ── Company info validation (Business flow only) ─────────────────
        // When the account has more than one business, the company fields only render after one is picked from the selector, so flag that case too or the user is stuck with no explanation.
        public bool BusinessSelectionMissing => ShowingBusiness && Businesses.Count > 1 && string.IsNullOrEmpty(SelectedBusinessId);
        public bool CompanyNameMissing => ShowingBusiness && string.IsNullOrWhiteSpace(OrgName);
        public bool CompanyCountryMissing => ShowingBusiness && string.IsNullOrWhiteSpace(OrgCountry);

        // The checkout API derives currency/tax from the billing address, so a missing address is required for Business checkout, not optional.
        public bool AddressMissing => ShowingBusiness && string.IsNullOrWhiteSpace(Address);

        public bool BusinessInfoMissing => BusinessSelectionMissing || CompanyNameMissing || CompanyCountryMissing || AddressMissing;

        // True after a failed validation attempt (checkbox or checkout click); individual field errors clear themselves as soon as that field is filled in.
        public bool ShowCompanyValidationErrors { get; private set; }

        public static IReadOnlyList<(string Code, string Name)> Countries => _countryList;

        // ── Member seats (Business only) ────────────────────────────────
        public int MembersCount { get; private set; } = 1;
        public bool PriceLoading { get; private set; }

        // ── Discount code ───────────────────────────────────────────────
        public string DiscountCode { get; private set; } = string.Empty;

        // Null = not yet applied; true = valid discount; false = invalid/unrecognised code.
        public bool? DiscountApplied { get; private set; }
        public bool DiscountLoading { get; private set; }

        // Updates the code on every keystroke instead of relying on the input's blur/change event, so a fast "Apply" click can't race a stale/empty code.
        public void UpdateDiscountCode(string value)
        {
            DiscountCode = value;
            DiscountApplied = null;
        }

        // ── Terms & checkout gating ─────────────────────────────────────
        public bool TermsAccepted { get; private set; }
        public bool CheckoutDisabled => !TermsAccepted || BusinessInfoMissing;
        public bool TrialEligible => IsTrialEligible(YearlyBilling, PricingLevel);

        // Gate for the "I accept" checkbox: for the Business flow this also validates Company name and Country, reverting the check and revealing inline errors if either is missing.
        public void SetTermsAccepted(bool accepted)
        {
            if (accepted && BusinessInfoMissing)
            {
                TermsAccepted = false;
                RevealCompanyValidationErrors();
                return;
            }

            TermsAccepted = accepted;
            if (accepted)
            {
                ShowCompanyValidationErrors = false;
            }
            UpdateViewState();
        }

        // Turns on the inline field errors and expands the Company information section so a collapsed "pre-filled" header can't hide the fields the error points at.
        private void RevealCompanyValidationErrors()
        {
            ShowCompanyValidationErrors = true;
            CompanyInfoExpanded = true;
            UpdateViewState();
        }

        // Defence-in-depth for the checkout/PayPal buttons in case they're ever reachable while Business info is incomplete; returns false when checkout should be blocked.
        public bool ValidateBeforeCheckout()
        {
            if (!BusinessInfoMissing)
            {
                return true;
            }

            RevealCompanyValidationErrors();
            return false;
        }

        // ── Checkout / activation overlay state — shared by paid checkout and Free-plan activation ──
        public bool CheckoutPending { get; private set; }
        public bool CheckoutSuccess { get; private set; }
        public string? CheckoutError { get; private set; }

        // True once a pending checkout/activation has sat unresolved for 15s, offering a "Log in" fallback so the user isn't stuck looking at a wheel with no way out.
        public bool ShowLoginFallback { get; private set; }

        // Overlay copy adapts to whichever flow is in progress (paid checkout vs. Free activation).
        public string OverlayWaitingTitle => _isFreeActivationFlow ? "Setting up your Free plan…" : "Waiting for payment…";

        public string OverlayWaitingMessage => _isFreeActivationFlow
            ? "This only takes a moment."
            : "Complete your payment in the browser window that just opened. This dialog updates automatically.";

        public string OverlaySuccessMessage => _isFreeActivationFlow
            ? "Your Free account is now active. Taking you to the app…"
            : "Your account is now active. Taking you to the app…";

        // "I've completed payment" only makes sense for the paid-checkout flow.
        public bool ShowCheckNowButton => !_isFreeActivationFlow;

        // ── Display strings ─────────────────────────────────────────────
        public string PageTitle =>
            ShowingBusiness ? "Start your Business plan"
            : FreePlanAvailable && IsFromSignup ? "Start your 14-day free trial"
            : FreePlanAvailable ? "Upgrade your plan"
            : "Your Premium plan";

        public string StepChipText =>
            FreePlanAvailable && IsFromSignup ? "START YOUR FREE TRIAL" : "UPGRADE YOUR PLAN";

        public string PremiumPriceText => GetAmount(YearlyBilling, SubscriptionLevel.Premium);
        public string PremiumPeriodText => YearlyBilling ? "/ year" : "/ month";
        public string PremiumMonthlyEquivalentText => GetAmount(false, SubscriptionLevel.Premium);

        public string BusinessPriceText => GetAmount(YearlyBilling, SubscriptionLevel.Business);
        public string BusinessPeriodText => YearlyBilling ? "/ user / year" : "/ user / month";
        public string BusinessMonthlyEquivalentText => GetAmount(false, SubscriptionLevel.Business);

        public string ReviewPlanName =>
            ShowingBusiness
                ? $"Business {(YearlyBilling ? "Yearly" : "Monthly")}"
                : $"Premium {(YearlyBilling ? "Yearly" : "Monthly")}";

        public string ReviewBillingText => YearlyBilling ? "Yearly billing" : "Monthly billing";

        public string ReviewSubtotalText =>
            ShowingBusiness ? $"{GetAmount(YearlyBilling, PricingLevel)} / user" : GetAmount(YearlyBilling, PricingLevel);

        public string ReviewAfterTrialText =>
            ShowingBusiness
                ? (YearlyBilling ? $"{GetAmount(YearlyBilling, PricingLevel)} / user / year" : $"{GetAmount(YearlyBilling, PricingLevel)} / user / month")
                : (YearlyBilling ? $"{GetAmount(YearlyBilling, PricingLevel)} / year" : $"{GetAmount(YearlyBilling, PricingLevel)} / month");

        // ── Discount renewal pricing ──────────────────────────────────────
        // True once a discount is applied and we have a pre-discount snapshot to compare against, so a first-period code gets its own renewal-price callout in the order summary.
        public bool HasActiveDiscount => DiscountApplied == true && _pricingModelsBeforeDiscount != null;

        private string PeriodLabel => YearlyBilling ? "year" : "month";

        // The regular (pre-discount) price for the selected period — what the plan renews at once the discounted first period ends.
        public string RenewalAmountText =>
            _pricingModelsBeforeDiscount
                ?.FirstOrDefault(p => p.SubscriptionMonths == (YearlyBilling ? 12 : 1) && p.SubscriptionLevel == PricingLevel)
                ?.Amount ?? string.Empty;

        // The per-user suffix used across the review/renewal strings for the Business plan.
        private string UserSuffix => ShowingBusiness ? " / user" : string.Empty;

        // "After 14-day trial" amount — folds in the renewal price once a discount is active, e.g. "$29.50 / first year · then $59.00 / year", instead of just the discounted amount.
        public string ReviewAfterTrialDisplayText =>
            HasActiveDiscount && !string.IsNullOrEmpty(RenewalAmountText)
                ? $"{GetAmount(YearlyBilling, PricingLevel)}{UserSuffix} / first {PeriodLabel} · then {RenewalAmountText}{UserSuffix} / {PeriodLabel}"
                : ReviewAfterTrialText;

        // The label shown once a code has been applied, e.g. "SAVE20 applied".
        public string AppliedDiscountLabel => $"{DiscountCode.ToUpperInvariant()} applied";

        // "Discount applied! You save $X / year" — falls back to a generic message if the saved amount can't be computed from the pricing strings (unexpected currency format, etc).
        public string DiscountSavingsBannerText
        {
            get
            {
                string? amount = DiscountSavingsAmountText;
                return !string.IsNullOrEmpty(amount)
                    ? $"Discount applied! You save {amount} / {PeriodLabel}"
                    : "Discount applied successfully!";
            }
        }

        // The actual amount saved by the discount for the selected period, e.g. "$29.50".
        private string? DiscountSavingsAmountText
        {
            get
            {
                if (!HasActiveDiscount) return null;

                decimal? before = ParseAmountValue(RenewalAmountText);
                decimal? after = ParseAmountValue(GetAmount(YearlyBilling, PricingLevel));
                if (before == null || after == null || before <= after) return null;

                string prefix = ExtractCurrencyPrefix(RenewalAmountText);
                return $"{prefix}{(before.Value - after.Value):0.00}";
            }
        }

        // Extracts the numeric value from a pricing string such as "$29.50".
        private static decimal? ParseAmountValue(string amount)
        {
            if (string.IsNullOrWhiteSpace(amount)) return null;
            string cleaned = Regex.Replace(amount, "[^0-9.]", "");
            return decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal value)
                ? value
                : (decimal?)null;
        }

        // Extracts the leading currency symbol from a pricing string such as "$29.50" -> "$".
        private static string ExtractCurrencyPrefix(string amount) =>
            Regex.Match(amount, @"^[^\d]*").Value.Trim();

        public string CtaText => TrialEligible ? "Start Free Trial" : "Subscribe Now";

        public string AmountVatText => GetVatDisplay(YearlyBilling, PricingLevel);

        // ── Lifecycle ────────────────────────────────────────────────────
        // Called once from the page's OnInitializedAsync; sets up plan pinning, country, pricing and business list.
        public async Task InitializeAsync(string? planQuery)
        {
            // Pre-populate company info from the signup flow, if available.
            if (!string.IsNullOrWhiteSpace(_signUpVm.OrganizationName))
            {
                OrgName = _signUpVm.OrganizationName;
                OrgCountry = _signUpVm.Country;
                OrgVat = _signUpVm.VatNumber;
                CompanyInfoFromSignup = true;
                CompanyInfoExpanded = false; // collapsed by default when pre-filled
            }

            // Auto-detect the country from IP if it's still unknown — signup always has one pre-filled, but a signed-in user relies entirely on this lookup, so fall back to the OS/culture region if it fails.
            if (string.IsNullOrEmpty(OrgCountry))
            {
                string detected = await GetCountryByIpAsync();
                OrgCountry = !string.IsNullOrEmpty(detected) ? detected : GetFallbackCountryFromCulture();
            }

            // ?plan=business (upgrade entry points) pre-selects the Business view — set before the pricing fetch below so PricingRequestOrigin reflects it on the very first request.
            SelectedPlan = (IsBusinessOrigin || string.Equals(planQuery, "business", StringComparison.OrdinalIgnoreCase))
                ? PlanMode.Business
                : null;

            // Premium and Business users are pinned to their origin plan.
            await GetPricingAsync(PricingRequestOrigin, DiscountCode, MembersCount, OrgCountry);
            PopulateBusinessAccounts();

            UpdateViewState();
        }

        // Cancels any in-flight polling/debounce work; called from the view's Dispose.
        public void CancelPendingOperations()
        {
            _pollCts?.Cancel();
            _pollCts?.Dispose();
            _pollCts = null;

            _memberDebounceCts?.Cancel();
            _memberDebounceCts?.Dispose();
            _memberDebounceCts = null;

            _loginFallbackCts?.Cancel();
            _loginFallbackCts?.Dispose();
            _loginFallbackCts = null;
        }

        // ── Pricing ──────────────────────────────────────────────────────
        // Returns the localised price amount string for the given billing period and plan level.
        public string GetAmount(bool yearly, SubscriptionLevel level)
        {
            if (_pricingModels == null) return string.Empty;
            return _pricingModels
                .FirstOrDefault(p => p.SubscriptionMonths == (yearly ? 12 : 1) && p.SubscriptionLevel == level)
                ?.Amount ?? string.Empty;
        }

        // Returns true when the user qualifies for a free trial on the given plan and billing period.
        public bool IsTrialEligible(bool yearly, SubscriptionLevel level) =>
            _pricingModels
                ?.FirstOrDefault(p => p.SubscriptionMonths == (yearly ? 12 : 1) && p.SubscriptionLevel == level)
                ?.EligibleForFreeTrial ?? false;

        // Returns a display-ready VAT string; falls back to "Calculated at checkout" when unavailable.
        public string GetVatDisplay(bool yearly, SubscriptionLevel level)
        {
            string? vat = _pricingModels
                ?.FirstOrDefault(p => p.SubscriptionMonths == (yearly ? 12 : 1) && p.SubscriptionLevel == level)
                ?.AmountVat;
            return string.IsNullOrWhiteSpace(vat) || vat == "0.00" ? "Calculated at checkout" : vat;
        }

        // Returns the full pricing model for the given period and level (needed for PayPal session creation).
        public PricingInfoApiModel? GetPriceInfo(bool yearly, SubscriptionLevel level) =>
            _pricingModels?.FirstOrDefault(p => p.SubscriptionMonths == (yearly ? 12 : 1) && p.SubscriptionLevel == level);

        // True when the last pricing fetch failed, surfaced so the page can show a clear retry option instead of silently staying blank.
        public bool PricingLoadFailed { get; private set; }
        public bool IsPricingLoading { get; private set; }

        // True once at least one pricing response has been loaded; a refetch keeps prior data in place while in flight, so only the very first load shows the full-page loading wheel.
        public bool HasPricingData => _pricingModels != null && _pricingModels.Any();

        // Fetches all available pricing models from the server and refreshes local state.
        public async Task GetPricingAsync(SignUpFrom signUpFrom, string discountCode, int members, string country)
        {
            IsPricingLoading = true;
            PricingLoadFailed = false;
            UpdateViewState();

            try
            {
                string ipAddress = GetLocalIPViaDns();
                LogOnIdentity identity = GetLogOnIdentity();
                IAccountService accountService = New<LogOnIdentity, IAccountService>(identity);
                _pricingModels = await accountService.GetPurchasePricingAsync(
                    signUpFrom.ToString(), discountCode, ipAddress, members, country)
                    ?? Enumerable.Empty<PricingInfoApiModel>();

                // An empty response is as good as a failure from the page's point of view — nothing to show either way.
                PricingLoadFailed = !_pricingModels.Any();
            }
            catch
            {
                _pricingModels = Enumerable.Empty<PricingInfoApiModel>();
                PricingLoadFailed = true;
            }
            finally
            {
                IsPricingLoading = false;
                UpdateViewState();
            }
        }

        // Re-fetches pricing using the view model's own current selection state.
        private Task RefreshPricingAsync() => GetPricingAsync(PricingRequestOrigin, DiscountCode, MembersCount, OrgCountry);

        // Retries the last pricing fetch — bound to the "Try again" action on the load-failure banner.
        public Task RetryPricingLoadAsync() => RefreshPricingAsync();

        // ── Members (Business seat count) ───────────────────────────────
        public async Task ChangeMembersAsync(int delta)
        {
            MembersCount = Math.Max(1, MembersCount + delta);
            UpdateViewState();
            await DebouncedRefreshPricingAsync();
        }

        // Waits for rapid +/- clicks to settle, then re-fetches pricing and clears the loading spinner.
        private async Task DebouncedRefreshPricingAsync()
        {
            _memberDebounceCts?.Cancel();
            _memberDebounceCts?.Dispose();
            _memberDebounceCts = new CancellationTokenSource();
            CancellationToken token = _memberDebounceCts.Token;

            PriceLoading = true;
            UpdateViewState();

            try
            {
                await Task.Delay(MemberDebounceMs, token);
                await RefreshPricingAsync();
            }
            catch (OperationCanceledException)
            {
                // A newer click superseded this debounce — the latest call will finish the work.
                return;
            }

            PriceLoading = false;
            UpdateViewState();
        }

        // ── Discount code ────────────────────────────────────────────────
        public async Task ApplyDiscountAsync()
        {
            if (string.IsNullOrWhiteSpace(DiscountCode))
            {
                DiscountApplied = false;
                _pricingModelsBeforeDiscount = null;
                UpdateViewState();
                return;
            }

            DiscountLoading = true;
            DiscountApplied = null;
            UpdateViewState();

            // Snapshot the current (undiscounted) pricing for the renewal price — the whole set, not just the current period, so a later Monthly/Yearly toggle doesn't go stale.
            IEnumerable<PricingInfoApiModel>? modelsBeforeDiscount = _pricingModels;
            string priceBefore = GetAmount(YearlyBilling, PricingLevel);

            await RefreshPricingAsync();
            string priceAfter = GetAmount(YearlyBilling, PricingLevel);

            // A discount is considered valid when the returned price differs from the pre-apply price.
            bool applied = !string.IsNullOrEmpty(priceAfter) && priceAfter != priceBefore;
            DiscountApplied = applied;
            _pricingModelsBeforeDiscount = applied ? modelsBeforeDiscount : null;
            DiscountLoading = false;
            UpdateViewState();
        }

        // Clears a previously applied (or failed) discount code and re-fetches standard pricing, so a code can't get stuck applied with no way back.
        public async Task RemoveAppliedDiscountAsync()
        {
            DiscountCode = string.Empty;
            DiscountApplied = null;
            _pricingModelsBeforeDiscount = null;
            UpdateViewState();
            await RefreshPricingAsync();
        }

        // ── Business account selection ──────────────────────────────────
        private void PopulateBusinessAccounts()
        {
            Businesses = BusinessAccounts.ToList();

            if (Businesses.Count == 1)
            {
                // Only one business on the account — select it automatically.
                ApplyBusinessSelection(Businesses[0].Key, Businesses[0].Value);
            }
            // Zero businesses: fields stay empty and expanded (default state).
            // Multiple businesses: the dropdown is shown and nothing is auto-selected.
        }

        public void SelectBusinessAccount(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                SelectedBusinessId = string.Empty;
                OrgName = string.Empty;
                CompanyInfoFromSignup = false;
                CompanyInfoExpanded = true;
                UpdateViewState();
                return;
            }

            KeyValuePair<string, string> business = Businesses.FirstOrDefault(b => b.Key == id);
            ApplyBusinessSelection(business.Key, business.Value);
            UpdateViewState();
        }

        private void ApplyBusinessSelection(string id, string name)
        {
            SelectedBusinessId = id;
            OrgName = name; // pre-fill company name from the selection
            CompanyInfoFromSignup = true; // switches to the collapsible read-only header
            CompanyInfoExpanded = false;  // start collapsed
        }

        // ── Checkout ─────────────────────────────────────────────────────
        public async Task<bool> CheckoutAsync()
        {
            _isFreeActivationFlow = false;
            _preCheckoutLevel = _userService.SubscriptionLevel;
            CheckoutError = null;
            CheckoutSuccess = false;
            CheckoutPending = true;
            StartLoginFallbackWatchdog();
            UpdateViewState();

            try
            {
                await RedirectToCheckoutSession(BuildPurchaseInfo());
                StartPaymentPolling(_preCheckoutLevel);
                return true;
            }
            catch
            {
                CheckoutPending = false;
                CheckoutError = "Could not open the checkout page. Please try again.";
                StopLoginFallbackWatchdog();
                UpdateViewState();
                return false;
            }
        }

        public async Task<bool> PayPalCheckoutAsync()
        {
            _isFreeActivationFlow = false;
            _preCheckoutLevel = _userService.SubscriptionLevel;
            CheckoutError = null;
            CheckoutSuccess = false;
            CheckoutPending = true;
            StartLoginFallbackWatchdog();
            UpdateViewState();

            try
            {
                await RedirectToPayPalCheckoutSession(YearlyBilling, PricingLevel);
                StartPaymentPolling(_preCheckoutLevel);
                return true;
            }
            catch
            {
                CheckoutPending = false;
                CheckoutError = "Could not open the PayPal checkout page. Please try again.";
                StopLoginFallbackWatchdog();
                UpdateViewState();
                return false;
            }
        }

        private PurchaseInfoApiModel BuildPurchaseInfo() => new PurchaseInfoApiModel
        {
            SubscriptionLevel = PricingLevel,
            SubscriptionMonths = YearlyBilling ? 12 : 1,
            FilledDiscountCode = DiscountCode,
            EligibleForFreeTrial = TrialEligible,
            OrganizationName = OrgName,
            Country = OrgCountry,
            EuVatNumber = OrgVat,
            Address = Address,
            PhoneNumber = PhoneNumber,
            InvoiceEmail = InvoiceEmail,
            Members = MembersCount,
            BusinessSubscriptionId = SelectedBusinessId
        };

        // Opens the Stripe checkout page in the system browser.
        public async Task RedirectToCheckoutSession(PurchaseInfoApiModel model)
        {
            model.UserEmail = _registerViewModel.CreateAccountModel.UserEmail;
            LogOnIdentity identity = GetLogOnIdentity();
            IAccountService accountService = New<LogOnIdentity, IAccountService>(identity);
            Uri url = await accountService.GetStripeCheckoutSessionUrlAsync(model);
            New<Abstractions.IBrowser>().OpenUri(url);
        }

        // Opens the PayPal checkout page for the specified plan in the system browser.
        public async Task RedirectToPayPalCheckoutSession(bool yearly, SubscriptionLevel level)
        {
            PricingInfoApiModel? info = GetPriceInfo(yearly, level);
            if (info == null) return;
            string ipAddress = GetLocalIPViaDns();
            LogOnIdentity identity = GetLogOnIdentity();
            IAccountService accountService = New<LogOnIdentity, IAccountService>(identity);
            string url = await accountService.GetPayPalCheckoutSessionUrlAsync(info.SubscriptionMonths, info.Currency, ipAddress);
            New<Abstractions.IBrowser>().OpenUri(new Uri(url));
        }

        // ── Payment polling ──────────────────────────────────────────────
        private void StartPaymentPolling(SubscriptionLevel levelBeforeCheckout)
        {
            _pollCts?.Cancel();
            _pollCts?.Dispose();
            _pollCts = new CancellationTokenSource();
            CancellationToken token = _pollCts.Token;

            _ = Task.Run(() => PollForPaymentAsync(levelBeforeCheckout, token), token);
        }

        // Polls every few seconds waiting for the subscription level to change, then signals navigation.
        private async Task PollForPaymentAsync(SubscriptionLevel levelBeforeCheckout, CancellationToken token)
        {
            try
            {
                for (int attempt = 0; attempt < MaxPollAttempts; attempt++)
                {
                    await Task.Delay(PollIntervalMs, token);

                    if (await CheckPaymentSucceededAsync(levelBeforeCheckout))
                    {
                        CheckoutSuccess = true;
                        CheckoutPending = false;
                        StopLoginFallbackWatchdog();
                        UpdateViewState();
                        await Task.Delay(PostSuccessDelayMs, token);
                        PrepareForHomeNavigation();
                        RequestNavigateHome?.Invoke();
                        return;
                    }
                }

                CheckoutPending = false;
                CheckoutError = "Payment confirmation timed out. If you completed payment, restart the app to refresh your account.";
                StopLoginFallbackWatchdog();
                UpdateViewState();
            }
            catch (OperationCanceledException)
            {
                // Polling was cancelled — the page was dismissed or a new checkout started.
            }
        }

        // Immediately polls once — called when the user clicks "I've completed payment".
        public async Task<bool> CheckPaymentNowAsync()
        {
            CheckoutError = null;
            UpdateViewState();

            bool paid = await CheckPaymentSucceededAsync(_preCheckoutLevel);
            if (paid)
            {
                _pollCts?.Cancel();
                CheckoutSuccess = true;
                CheckoutPending = false;
                StopLoginFallbackWatchdog();
                PrepareForHomeNavigation();
            }
            else
            {
                CheckoutError = "Payment not confirmed yet. Please wait a moment and try again, or check your email for a receipt.";
            }

            UpdateViewState();
            return paid;
        }

        // Polls the account service and returns true once the subscription level has changed, re-syncing the shared UserService cache (read by the sidebar/home page) only when a change is actually found.
        public async Task<bool> CheckPaymentSucceededAsync(SubscriptionLevel levelBeforeCheckout)
        {
            if (!await HasSubscriptionLevelChangedAsync(levelBeforeCheckout))
                return false;

            await RefreshSessionStateAsync();
            return true;
        }

        // Cheap live-status check used on every poll tick, without the full profile/UserService refresh.
        private async Task<bool> HasSubscriptionLevelChangedAsync(SubscriptionLevel levelBeforeCheckout)
        {
            try
            {
                AccountStatusViewModel statusVm = New<AccountStatusViewModel>();
                await statusVm.LoadAccountStatusAsync();
                return statusVm.SubscriptionLevel != levelBeforeCheckout
                    && statusVm.SubscriptionLevel != SubscriptionLevel.Unknown;
            }
            catch
            {
                return false;
            }
        }

        public void DismissCheckoutOverlay()
        {
            _pollCts?.Cancel();
            CheckoutPending = false;
            CheckoutSuccess = false;
            CheckoutError = null;
            StopLoginFallbackWatchdog();
            UpdateViewState();
        }

        // ── Free plan activation ─────────────────────────────────────────
        // Shares the checkout overlay's pending/success/error state so the user gets the same clear feedback (and 15s "Log in" fallback) as the paid-checkout flow.
        public async Task<bool> ActivateFreeAsync()
        {
            _isFreeActivationFlow = true;
            CheckoutError = null;
            CheckoutSuccess = false;
            CheckoutPending = true;
            StartLoginFallbackWatchdog();
            UpdateViewState();

            try
            {
                await SetViewerPlanAsync();

                // Sync the shared UserService cache immediately, or the home page keeps gating paid content on the stale pre-activation subscription level.
                await RefreshSessionStateAsync();

                CheckoutSuccess = true;
                CheckoutPending = false;
                StopLoginFallbackWatchdog();
                UpdateViewState();

                await Task.Delay(PostSuccessDelayMs);
                PrepareForHomeNavigation();
                RequestNavigateHome?.Invoke();
                return true;
            }
            catch
            {
                CheckoutPending = false;
                CheckoutError = "Could not activate the Free plan. Please try again.";
                StopLoginFallbackWatchdog();
                UpdateViewState();
                return false;
            }
        }

        // Reloads account status and syncs the shared UserService cache so the rest of the app (sidebar, paid-feature gates, home page) reflects a plan change immediately.
        private async Task RefreshSessionStateAsync()
        {
            await _profileViewModel.InitializeAsync();
            _userService.InitializeData(_profileViewModel.Account.SubscriptionLevel, _profileViewModel.Account.UserEmail);
        }

        // Un-suppresses the login form before heading home — a no-op when genuinely logged in, but restores the Login fallback if an earlier step (e.g. OTP/free-plan signup) left it suppressed.
        private void PrepareForHomeNavigation()
        {
            _logOnViewModel.IsVisible = true;
        }

        // Recovers from a stuck/ambiguous session on a "Log in" fallback click: signs out if genuinely already signed in (a stale state), otherwise un-suppresses the login form; returns true when the razor should navigate home.
        public async Task<bool> RecoverSessionAsync()
        {
            if (_logOnViewModel.IsLoggedOn)
            {
                await _profileViewModel.SignOut();
                return false;
            }

            _logOnViewModel.IsVisible = true;
            return true;
        }

        // ── Login fallback watchdog ──────────────────────────────────────
        // Shows a "Log in" link on the overlay if a pending checkout/activation hasn't resolved within 15s, so the user always has a way out instead of watching a wheel indefinitely.
        private void StartLoginFallbackWatchdog()
        {
            ShowLoginFallback = false;
            _loginFallbackCts?.Cancel();
            _loginFallbackCts?.Dispose();
            _loginFallbackCts = new CancellationTokenSource();
            CancellationToken token = _loginFallbackCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(LoginFallbackDelayMs, token);
                    ShowLoginFallback = true;
                    UpdateViewState();
                }
                catch (OperationCanceledException)
                {
                    // Resolved (or dismissed) before the fallback delay elapsed.
                }
            }, token);
        }

        private void StopLoginFallbackWatchdog()
        {
            _loginFallbackCts?.Cancel();
            _loginFallbackCts?.Dispose();
            _loginFallbackCts = null;
            ShowLoginFallback = false;
        }

        // Activates the free (viewer) plan for the signed-in user.
        public async Task SetViewerPlanAsync()
        {
            LogOnIdentity identity = GetLogOnIdentity();
            IAccountService accountService = New<LogOnIdentity, IAccountService>(identity);
            await accountService.SetMyAccountViewerPlanAsync();
        }

        // ── External navigation ──────────────────────────────────────────
        public void OpenExternalLink(string url) => New<Abstractions.IBrowser>().OpenUri(new Uri(url));

        // ── Helpers ──────────────────────────────────────────────────────
        // Detects the user's country from their public IP via ipinfo.io; returns ISO 3166-1 alpha-2 or empty string.
        public async Task<string> GetCountryByIpAsync()
        {
            try
            {
                using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                string json = await client.GetStringAsync("https://ipinfo.io/json");
                Match match = Regex.Match(json, "\"country\"\\s*:\\s*\"([A-Z]{2})\"");
                if (match.Success) return match.Groups[1].Value;
            }
            catch { }
            return string.Empty;
        }

        // Last-resort country guess from the OS/culture region, used only when IP geolocation fails.
        private static string GetFallbackCountryFromCulture()
        {
            try
            {
                var region = new System.Globalization.RegionInfo(System.Globalization.CultureInfo.CurrentCulture.Name);
                return region.TwoLetterISORegionName;
            }
            catch
            {
                return "US";
            }
        }

        private LogOnIdentity GetLogOnIdentity()
        {
            LogOnIdentity identity = New<KnownIdentities>().DefaultEncryptionIdentity;
            if (identity == LogOnIdentity.Empty)
                identity = new LogOnIdentity(
                    EmailAddress.Parse(_registerViewModel.CreateAccountModel.UserEmail),
                    new Passphrase(_registerViewModel.CreateAccountModel.PasswordText));
            return identity;
        }

        // Returns the machine's local IPv4 address used as a region hint for the pricing API.
        private static string GetLocalIPViaDns()
        {
            try
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                return host.AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    ?.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        // Static reference data for the business "Country" dropdown.
        private static List<(string Code, string Name)> BuildCountryList() => new()
        {
            ("AF", "Afghanistan"), ("AL", "Albania"), ("DZ", "Algeria"), ("AD", "Andorra"),
            ("AO", "Angola"), ("AG", "Antigua and Barbuda"), ("AR", "Argentina"), ("AM", "Armenia"),
            ("AU", "Australia"), ("AT", "Austria"), ("AZ", "Azerbaijan"), ("BS", "Bahamas"),
            ("BH", "Bahrain"), ("BD", "Bangladesh"), ("BB", "Barbados"), ("BY", "Belarus"),
            ("BE", "Belgium"), ("BZ", "Belize"), ("BJ", "Benin"), ("BT", "Bhutan"),
            ("BO", "Bolivia"), ("BA", "Bosnia and Herzegovina"), ("BW", "Botswana"), ("BR", "Brazil"),
            ("BN", "Brunei"), ("BG", "Bulgaria"), ("BF", "Burkina Faso"), ("BI", "Burundi"),
            ("CV", "Cabo Verde"), ("KH", "Cambodia"), ("CM", "Cameroon"), ("CA", "Canada"),
            ("CF", "Central African Republic"), ("TD", "Chad"), ("CL", "Chile"), ("CN", "China"),
            ("CO", "Colombia"), ("KM", "Comoros"), ("CG", "Congo"), ("CD", "Congo (DRC)"),
            ("CR", "Costa Rica"), ("HR", "Croatia"), ("CU", "Cuba"), ("CY", "Cyprus"),
            ("CZ", "Czech Republic"), ("DK", "Denmark"), ("DJ", "Djibouti"), ("DM", "Dominica"),
            ("DO", "Dominican Republic"), ("EC", "Ecuador"), ("EG", "Egypt"), ("SV", "El Salvador"),
            ("GQ", "Equatorial Guinea"), ("ER", "Eritrea"), ("EE", "Estonia"), ("SZ", "Eswatini"),
            ("ET", "Ethiopia"), ("FJ", "Fiji"), ("FI", "Finland"), ("FR", "France"),
            ("GA", "Gabon"), ("GM", "Gambia"), ("GE", "Georgia"), ("DE", "Germany"),
            ("GH", "Ghana"), ("GR", "Greece"), ("GD", "Grenada"), ("GT", "Guatemala"),
            ("GN", "Guinea"), ("GW", "Guinea-Bissau"), ("GY", "Guyana"), ("HT", "Haiti"),
            ("HN", "Honduras"), ("HU", "Hungary"), ("IS", "Iceland"), ("IN", "India"),
            ("ID", "Indonesia"), ("IR", "Iran"), ("IQ", "Iraq"), ("IE", "Ireland"),
            ("IL", "Israel"), ("IT", "Italy"), ("JM", "Jamaica"), ("JP", "Japan"),
            ("JO", "Jordan"), ("KZ", "Kazakhstan"), ("KE", "Kenya"), ("KI", "Kiribati"),
            ("KP", "Korea (North)"), ("KR", "Korea (South)"), ("KW", "Kuwait"), ("KG", "Kyrgyzstan"),
            ("LA", "Laos"), ("LV", "Latvia"), ("LB", "Lebanon"), ("LS", "Lesotho"),
            ("LR", "Liberia"), ("LY", "Libya"), ("LI", "Liechtenstein"), ("LT", "Lithuania"),
            ("LU", "Luxembourg"), ("MG", "Madagascar"), ("MW", "Malawi"), ("MY", "Malaysia"),
            ("MV", "Maldives"), ("ML", "Mali"), ("MT", "Malta"), ("MH", "Marshall Islands"),
            ("MR", "Mauritania"), ("MU", "Mauritius"), ("MX", "Mexico"), ("FM", "Micronesia"),
            ("MD", "Moldova"), ("MC", "Monaco"), ("MN", "Mongolia"), ("ME", "Montenegro"),
            ("MA", "Morocco"), ("MZ", "Mozambique"), ("MM", "Myanmar"), ("NA", "Namibia"),
            ("NR", "Nauru"), ("NP", "Nepal"), ("NL", "Netherlands"), ("NZ", "New Zealand"),
            ("NI", "Nicaragua"), ("NE", "Niger"), ("NG", "Nigeria"), ("MK", "North Macedonia"),
            ("NO", "Norway"), ("OM", "Oman"), ("PK", "Pakistan"), ("PW", "Palau"),
            ("PA", "Panama"), ("PG", "Papua New Guinea"), ("PY", "Paraguay"), ("PE", "Peru"),
            ("PH", "Philippines"), ("PL", "Poland"), ("PT", "Portugal"), ("QA", "Qatar"),
            ("RO", "Romania"), ("RU", "Russia"), ("RW", "Rwanda"), ("KN", "Saint Kitts and Nevis"),
            ("LC", "Saint Lucia"), ("VC", "Saint Vincent and the Grenadines"), ("WS", "Samoa"),
            ("SM", "San Marino"), ("ST", "Sao Tome and Principe"), ("SA", "Saudi Arabia"),
            ("SN", "Senegal"), ("RS", "Serbia"), ("SC", "Seychelles"), ("SL", "Sierra Leone"),
            ("SG", "Singapore"), ("SK", "Slovakia"), ("SI", "Slovenia"), ("SB", "Solomon Islands"),
            ("SO", "Somalia"), ("ZA", "South Africa"), ("SS", "South Sudan"), ("ES", "Spain"),
            ("LK", "Sri Lanka"), ("SD", "Sudan"), ("SR", "Suriname"), ("SE", "Sweden"),
            ("CH", "Switzerland"), ("SY", "Syria"), ("TW", "Taiwan"), ("TJ", "Tajikistan"),
            ("TZ", "Tanzania"), ("TH", "Thailand"), ("TL", "Timor-Leste"), ("TG", "Togo"),
            ("TO", "Tonga"), ("TT", "Trinidad and Tobago"), ("TN", "Tunisia"), ("TR", "Turkey"),
            ("TM", "Turkmenistan"), ("TV", "Tuvalu"), ("UG", "Uganda"), ("UA", "Ukraine"),
            ("AE", "United Arab Emirates"), ("GB", "United Kingdom"), ("US", "United States"),
            ("UY", "Uruguay"), ("UZ", "Uzbekistan"), ("VU", "Vanuatu"), ("VE", "Venezuela"),
            ("VN", "Vietnam"), ("YE", "Yemen"), ("ZM", "Zambia"), ("ZW", "Zimbabwe"),
        };
    }
}
