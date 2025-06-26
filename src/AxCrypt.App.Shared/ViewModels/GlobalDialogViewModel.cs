using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Common;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels;

public class GlobalDialogViewModel
{
    private readonly Queue<PopupRequest> _popupQueue = new();
    private bool _isPopupActive = false;

    public GlobalDialogViewModel()
    {
        LogOnViewModel = AxCServiceProviderExtension.GetService<LogOnViewModel>();
        LogOnViewModel.PopupButtons = [PopupButtons.None];
    }

    public GlobalDialogViewModel(string title, string message, DoNotShowAgainOptions dontShowAgain)
    {
        Title = title;
        MessageText = message;
        DontShowAgainOptions = dontShowAgain;
    }

    public string? Title { get; set; }

    public string? MessageText { get; set; }

    public DoNotShowAgainOptions DontShowAgainOptions { get; set; }

    public bool IsCheckboxDontShowThisAgain { get; set; }

    public LogOnViewModel? LogOnViewModel { get; set; }

    public Task<PopupButtons> ShowPopupDialog(PopupButtons[] buttons, string title, string message, DoNotShowAgainOptions dontShowAgain)
    {
        PopupRequest request = new PopupRequest
        {
            Buttons = buttons,
            Title = title,
            Message = message,
            DoNotShow = dontShowAgain
        };

        lock (_popupQueue)
        {
            _popupQueue.Enqueue(request);
        }

        _ = ProcessPopupQueueAsync();

        return request.Completion.Task;
    }

    private async Task ProcessPopupQueueAsync()
    {
        if (_isPopupActive)
            return;

        while (true)
        {
            PopupRequest? nextRequest = null;

            lock (_popupQueue)
            {
                if (_popupQueue.Count == 0)
                {
                    _isPopupActive = false;
                    return;
                }

                _isPopupActive = true;
                nextRequest = _popupQueue.Dequeue();
            }

            try
            {
                LogOnViewModel!.GlobalViewModel = new GlobalDialogViewModel(nextRequest.Title, nextRequest.Message, nextRequest.DoNotShow);
                LogOnViewModel.PopupResult = DialogResult.None;
                LogOnViewModel.PopupButtons = nextRequest.Buttons;

                if ((New<UserSettings>().DoNotShowAgain & nextRequest.DoNotShow) != 0)
                {
                    nextRequest.Completion.TrySetResult(nextRequest.Buttons[0]);
                    continue;
                }

                LogOnViewModel.GlobalPopupDialog.Show();

                while (LogOnViewModel.PopupResult == DialogResult.None)
                {
                    await Task.Delay(1000); 
                }

                LogOnViewModel.GlobalPopupDialog.Close();

                nextRequest.Completion.TrySetResult(LogOnViewModel.PopupButtons!.FirstOrDefault());
            }
            catch (Exception ex)
            {
                nextRequest?.Completion.TrySetException(ex);
            }
        }
    }

    public void Button_OkClicked()
    {
        if (LogOnViewModel!.GlobalViewModel!.DontShowAgainOptions != DoNotShowAgainOptions.None && IsCheckboxDontShowThisAgain)
        {
            New<UserSettings>().DoNotShowAgain = New<UserSettings>().DoNotShowAgain | LogOnViewModel.GlobalViewModel.DontShowAgainOptions!;
        }

        LogOnViewModel!.PopupResult = DialogResult.OK;
        LogOnViewModel.PopupButtons = [PopupButtons.Ok];
    }

    public void Button_CancelClicked()
    {
        if (LogOnViewModel!.GlobalViewModel!.DontShowAgainOptions != DoNotShowAgainOptions.None && IsCheckboxDontShowThisAgain)
        {
            New<UserSettings>().DoNotShowAgain = (DoNotShowAgainOptions)(New<UserSettings>().DoNotShowAgain | LogOnViewModel.GlobalViewModel.DontShowAgainOptions)!;
        }

        LogOnViewModel!.PopupResult = DialogResult.Cancel;
        LogOnViewModel.PopupButtons = [PopupButtons.Cancel];
    }

    private class PopupRequest
    {
        public TaskCompletionSource<PopupButtons> Completion { get; set; } = new();
        public PopupButtons[]? Buttons { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public DoNotShowAgainOptions DoNotShow { get; set; }
    }
}