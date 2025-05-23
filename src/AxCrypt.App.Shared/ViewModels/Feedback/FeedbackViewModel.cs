using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Content;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels.Feedback;

public class FeedbackViewModel
{
    public FeedbackViewModel(LogOnViewModel logOnViewModel)
    {
        AllSubject = Enum.GetValues(typeof(FeedbackSubject))
             .Cast<FeedbackSubject>()
             .ToList();
        LogOnViewModel = logOnViewModel;
    }

    public LogOnViewModel LogOnViewModel { get; set; }

    public string UserInput { get; set; } = string.Empty;

    public string ErrorMessage { get; set; }

    public FeedbackSubject SelectedSubject { get; set; }

    public List<FeedbackSubject> AllSubject { get; private set; }

    public void SelectSubject(FeedbackSubject subject)
    {
        SelectedSubject = subject;
    }

    public async Task HandleFormSubmit()
    {
        if (string.IsNullOrEmpty(UserInput))
        {
            ErrorMessage = "Please add something before submitting!";
            return;
        }

        // Perform form submission logic here
        //await SelectedSubjectChanged.InvokeAsync(SelectedSubject);
        // Optionally, reset form state or perform other actions

        IAccountService accountService = New<LogOnIdentity, IAccountService>(New<KnownIdentities>().DefaultEncryptionIdentity);
        await accountService.SendFeedbackAsync(SelectedSubject.ToString(), UserInput);

        ErrorMessage = string.Empty;
        UserInput = string.Empty;
        LogOnViewModel.FeedbackDialog.Close();
    }
}
