using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Content;
using AxCrypt.Core.UI.ViewModel;

namespace AxCrypt.App.Shared.ViewModels
{
    public class UserPromptViewModel : ViewModelBase
    {
        public LogOnViewModel LogOnViewModel;

        public string UserPrompt { get; private set; }
        public string[] PromptActions { get; private set; }
        public string PromptIcon { get; private set; }

        public UserPromptViewModel() 
        {
            LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
            PromptActions = [ Texts.ButtonYesText, Texts.ButtonNoText ];
            UserPrompt = Texts.AreYouSureText;
            PromptIcon = "images/ImgUpgrd.svg";
        }

        public DialogResult PageResult { get { return GetProperty<DialogResult>(nameof(PageResult)); } set { SetProperty(nameof(PageResult), value); } }

        public async Task SetUserPrompt(string prompt, string[] promptOptions, string promptIcon, Action OkAction)
        {
            UserPrompt = prompt;
            PromptActions = promptOptions;
            PromptIcon = promptIcon;


            PageResult = DialogResult.None;
            LogOnViewModel.UserPromptDialog.Show();

            while (PageResult == DialogResult.None)
            {
                await Task.Delay(1000);
            }

            if(PageResult == DialogResult.OK)
            {
                OkAction();
            }

            LogOnViewModel.UserPromptDialog.Close();
        }
    }
}
