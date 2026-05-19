using AxCrypt.App.Shared.Helpers;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.Code
{
    public class CopyToClipboardUtility
    {
        public string CopiedInfoText = "";

        public async Task CopyToClipboard(string copyText)
        {
            if (string.IsNullOrEmpty(copyText))
            {
                return;
            }

            await Clipboard.SetTextAsync(copyText);
            CopiedInfoText = "Copied!";
            AxCServiceProviderExtension.StatusAlertService!.Success("Copied to clipboard!");
            await Task.Delay(1500);
            CopiedInfoText = "";
            AxCServiceProviderExtension.StatusAlertService!.Hide();
        }
    }
}