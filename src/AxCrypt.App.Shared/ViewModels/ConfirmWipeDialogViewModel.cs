using AxCrypt.App.Shared.Services.UI;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Core.UI.ViewModel;

namespace AxCrypt.App.Shared.ViewModels;

public class ConfirmWipeDialogViewModel : ViewModelBase
{
    public ConfirmWipeDialogViewModel()
    {
        ConfirmWipeDialog = new CommonDialogService();
    }

    public CommonDialogService ConfirmWipeDialog { get { return GetProperty<CommonDialogService>(nameof(ConfirmWipeDialog)); } set { SetProperty(nameof(ConfirmWipeDialog), value); } }

    public async Task ShowHideConfirmWipeDialog()
    {   
        OptedYes = false;
        OptedNo = false;
        OptedCancel = false;
        OptedCheckAllFiles = false;
        
        ConfirmWipeDialog.Show();

        while (!OptedYes && !OptedNo && !OptedCancel)
        {
            await Task.Delay(1000);
        }

        ConfirmWipeDialog.Close();
    }

    public bool OptedCheckAllFiles { get; set; } = false;

    public bool OptedYes { get; set; } = false;

    public bool OptedNo { get; set; } = false;

    public bool OptedCancel { get; set; } = false;

    public string FileName {get; set; } = ""; 

    private DialogResult PageResult { get; set; } = DialogResult.None;
}

