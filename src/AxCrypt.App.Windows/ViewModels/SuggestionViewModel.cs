using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels
{
    public class SuggestionViewModel
    {
        public bool showSugPopup { get; set; }

        public DateTime lastClosedTime { get; set; }

        public void TogglePopup()
        {
            if ((DateTime.Now - lastClosedTime).TotalMinutes >= 5)
            {
                showSugPopup = !showSugPopup;
            }
        }

        public void ClosePopup()
        {
            showSugPopup = false;
            lastClosedTime = DateTime.Now;
        }

        public void DownloadMobileAppLink()
        {
            New<Abstractions.IBrowser>().OpenUri(new Uri("https://axcrypt.net/download/"));
        }

        public void PasswordGeneratorLink()
        {
            New<Abstractions.IBrowser>().OpenUri(new Uri("https://axcrypt.net/information/password-generator/"));
        }

        public void DownloadAndroidApp()
        {
            New<Abstractions.IBrowser>().OpenUri(new Uri("https://play.google.com/store/apps/details?id=net.axcrypt.axcrypt2x"));
        }

        public void DownloadiOsApp()
        {
            New<Abstractions.IBrowser>().OpenUri(new Uri("https://apps.apple.com/us/app/axcrypt/id1157695909"));
        }

        public void LearnMore()
        {
            New<Abstractions.IBrowser>().OpenUri(new Uri("https://axcrypt.net/information/requirements/"));
        }
    }
}
