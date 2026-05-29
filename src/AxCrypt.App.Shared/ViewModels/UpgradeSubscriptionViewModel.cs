using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.UI;
using AxCrypt.Content;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.ViewModels
{
    public class UpgradeSubscriptionViewModel : ViewModelBase
    {
        private readonly LogOnViewModel _logonViewModel;
        public UpgradeSubscriptionViewModel(LogOnViewModel logOnViewModel)
        {
            TryNowSubscriptionDialog = new CommonDialogService();
            TrySubscriptionDialog = new CommonDialogService();
            UpgradeSubscriptionDialog = new CommonDialogService();
            BusinessSubscriptionDialog = new CommonDialogService();

            _logonViewModel = logOnViewModel;
        }

        public CommonDialogService TryNowSubscriptionDialog
        { get { return GetProperty<CommonDialogService>(nameof(TryNowSubscriptionDialog)); } set { SetProperty(nameof(TryNowSubscriptionDialog), value); } }

        public CommonDialogService TrySubscriptionDialog
        { get { return GetProperty<CommonDialogService>(nameof(TrySubscriptionDialog)); } set { SetProperty(nameof(TrySubscriptionDialog), value); } }

        public CommonDialogService UpgradeSubscriptionDialog
        { get { return GetProperty<CommonDialogService>(nameof(UpgradeSubscriptionDialog)); } set { SetProperty(nameof(UpgradeSubscriptionDialog), value); } }

        /// <summary>
        /// Dedicated Business-tier conversion popup. Used in place of the
        /// generic <see cref="TryNowSubscriptionDialog"/> /
        /// <see cref="UpgradeSubscriptionDialog"/> when the signed-in user's
        /// <see cref="SignUpFrom"/> resolves to Business — because business
        /// admins respond to a different value proposition (audit log,
        /// MFA enforcement, group management, SOC/HIPAA) than the consumer
        /// upgrade copy. One popup, one tone, one funnel destination.
        /// </summary>
        public CommonDialogService BusinessSubscriptionDialog
        { get { return GetProperty<CommonDialogService>(nameof(BusinessSubscriptionDialog)); } set { SetProperty(nameof(BusinessSubscriptionDialog), value); } }

        /// <summary>
        /// True when the signed-in account originally signed up on Business.
        /// We surface the dedicated <see cref="BusinessSubscriptionDialog"/>
        /// in that case rather than the generic upgrade popups, so the
        /// IT-admin-facing copy and CTA stay consistent.
        /// </summary>
        private bool IsBusinessOriginated =>
            AxCServiceProviderExtension.GetService<UserService>()?.SignUpFrom == SignUpFrom.Business;

        /// <summary>
        /// Show the appropriate conversion dialog based on the user’s tier and trial eligibility.
        /// Copy and feature-card content are driven by ConversionDialogConfig factories.
        /// </summary>
        public void ShowUpgradeDialog()
        {
            if (IsBusinessOriginated)
            {
                BusinessSubscriptionDialog.Show();
                return;
            }

            if (_logonViewModel.EligibleForFreeTrial)
            {
                TrySubscriptionDialog.Show();
                return;
            }

            UpgradeSubscriptionDialog.Show();
        }

        /// <summary>
        /// Show the conversion dialog on first sign-in / re-engage nudge.
        /// No-ops for paid users — only shown to Free tier.
        /// </summary>
        public void ShowTryUpgradeDialog()
        {
            if (_logonViewModel.SubscriptionLevel > Api.Model.SubscriptionLevel.Free)
                return;

            if (IsBusinessOriginated)
            {
                BusinessSubscriptionDialog.Show();
                return;
            }

            if (_logonViewModel.EligibleForFreeTrial)
            {
                TryNowSubscriptionDialog.Show();
                return;
            }

            UpgradeSubscriptionDialog.Show();
        }

        public void OpenBusinessPage()
        {
            Core.BrowseUtility.RedirectToAccountWebUrl("{0}HomeUser/Login?eff=1&reqFrom=Business");
        }

        public void OpenPremiumPage()
        {
            Core.BrowseUtility.RedirectToAccountWebUrl("{0}HomeUser/Login?eff=1&reqFrom=Premium");
        }

        public void OpenPassswordManagerPage()
        {
            Core.BrowseUtility.RedirectToAccountWebUrl("{0}HomeUser/Login?eff=1&reqFrom=PasswordManager");
        }

        public void OpenPurchasePage()
        {
            Core.BrowseUtility.RedirectToAccountWebUrl("{0}HomeUser/Login?Signup=True");
        }

        public void OpenPricingPage()
        {
            Core.BrowseUtility.RedirectTo("https://axcrypt.net/pricing/");
        }
    }
}
