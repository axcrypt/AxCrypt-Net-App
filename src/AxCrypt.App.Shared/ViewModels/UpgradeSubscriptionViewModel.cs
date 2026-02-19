using AxCrypt.App.Shared.Services.UI;
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

            _logonViewModel = logOnViewModel;
        }

        public CommonDialogService TryNowSubscriptionDialog
        { get { return GetProperty<CommonDialogService>(nameof(TryNowSubscriptionDialog)); } set { SetProperty(nameof(TryNowSubscriptionDialog), value); } }

        public CommonDialogService TrySubscriptionDialog
        { get { return GetProperty<CommonDialogService>(nameof(TrySubscriptionDialog)); } set { SetProperty(nameof(TrySubscriptionDialog), value); } }

        public CommonDialogService UpgradeSubscriptionDialog
        { get { return GetProperty<CommonDialogService>(nameof(UpgradeSubscriptionDialog)); } set { SetProperty(nameof(UpgradeSubscriptionDialog), value); } }


        public void ShowUpgradeDialog()
        {
            if (_logonViewModel.EligibleForFreeTrial)
            {
                TrySubscriptionDialog.Show();
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
