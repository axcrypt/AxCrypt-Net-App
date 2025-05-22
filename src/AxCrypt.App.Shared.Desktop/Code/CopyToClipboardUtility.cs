using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace AxCrypt.App.Shared.Desktop.Code
{
	public class CopyToClipboardUtility
	{
        public string CopiedInfoText = "";

        public async Task CopyToClipboard(string copyText)
        {
            await Clipboard.SetTextAsync(copyText);
            CopiedInfoText = "Copied!";
            await Task.Delay(2000);
            CopiedInfoText = "";
        }
    }
}

