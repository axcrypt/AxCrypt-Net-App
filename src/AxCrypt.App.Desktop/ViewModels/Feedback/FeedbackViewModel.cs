using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Content;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.ViewModels.Feedback;

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
            return;
        }

        // Perform form submission logic here
        //await SelectedSubjectChanged.InvokeAsync(SelectedSubject);
        // Optionally, reset form state or perform other actions

        IAccountService accountService = New<LogOnIdentity, IAccountService>(New<KnownIdentities>().DefaultEncryptionIdentity);
        try
        {
            using (await New<IProgressDialog>().Show(Texts.ProgressIndicatorFeedbackMessage, Texts.ProgressIndicatorWaitMessage))
            {
                await accountService.SendFeedbackAsync(SelectedSubject.ToString(), UserInput);
            }

            UserInput = string.Empty;
            LogOnViewModel.FeedbackDialog.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
