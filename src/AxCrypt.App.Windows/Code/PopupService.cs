using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.App.Shared.Services;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.UI;

namespace AxCrypt.App.Windows.Code;

public class PopupService : IPopup
{
    private GlobalDialogViewModel? _globalViewModel;

    private static readonly PopupButtons[] possibleButtons = new PopupButtons[]
    {
        PopupButtons.Ok,
        PopupButtons.Cancel,
        PopupButtons.Exit,
    };

    public Task<PopupButtons> ShowAsync(PopupButtons buttons, string title, string message)
    {
        return ShowAsync(buttons, title, message, DoNotShowAgainOptions.None);
    }

    public async Task<PopupButtons> ShowAsync(PopupButtons buttons, string title, string message, DoNotShowAgainOptions doNotShowAgainOption, string doNotShowAgainCustomText)
    {
        PopupButtons[] activeButtons = possibleButtons.Where(b => buttons.HasFlag(b)).ToArray();
        bool isAccepted = false;

        switch (activeButtons.Length)
        {
            case 1:
                _globalViewModel = AxCServiceProvider.GetService<GlobalDialogViewModel>();
                await _globalViewModel.ShowVersionDialog(activeButtons[0], title, message, doNotShowAgainOption);
                return activeButtons[0];

            case 2:
                ButtonActions actions = new ButtonActions(activeButtons);
                string leftButton = ConvertToString(actions.AcceptAction);
                string rightButton = ConvertToString(actions.CancelAction);

                _globalViewModel = AxCServiceProvider.GetService<GlobalDialogViewModel>();
                await _globalViewModel.ShowVersionDialog(activeButtons[0], title, message, doNotShowAgainOption);
                isAccepted = _globalViewModel.LogOnViewModel!.PopupResult == Shared.Utility.DialogResult.OK;

                if (isAccepted)
                {
                    return actions.AcceptAction;
                }
                else
                {
                    return actions.CancelAction;
                }

            default:
                throw new NotSupportedException("Can display alerts with 1 or 2 buttons only");
        }
    }

    public Task<PopupButtons> ShowAsync(PopupButtons buttons, string title, string message, DoNotShowAgainOptions doNotShowAgain)
    {
        return ShowAsync(buttons, title, message, doNotShowAgain, null);
    }

    public Task<string> ShowAsync(string[] buttons, string title, string message)
    {
        return ShowAsync(buttons, title, message, DoNotShowAgainOptions.None);
    }

    public async Task<string> ShowAsync(string[] buttons, string title, string message, DoNotShowAgainOptions dontShowAgain)
    {
        bool isAccepted = false;
        _globalViewModel = AxCServiceProvider.GetService<GlobalDialogViewModel>();

        switch (buttons.Length)
        {
            case 1:
                _globalViewModel = AxCServiceProvider.GetService<GlobalDialogViewModel>();
                await _globalViewModel.ShowVersionDialog(possibleButtons[0], title, message, dontShowAgain);
                isAccepted = _globalViewModel.LogOnViewModel!.PopupResult == Shared.Utility.DialogResult.OK;
                return buttons[0];

            case 2:
                string leftButton = buttons[0];
                string rightButton = buttons[1];

                _globalViewModel = AxCServiceProvider.GetService<GlobalDialogViewModel>();
                await _globalViewModel.ShowVersionDialog(possibleButtons[0], title, message, dontShowAgain);
                isAccepted = _globalViewModel.LogOnViewModel!.PopupResult == Shared.Utility.DialogResult.OK;

                if (isAccepted)
                {
                    return leftButton;
                }
                else
                {
                    return rightButton;
                }

            default:
                throw new NotSupportedException("Can display alerts with 1 or 2 buttons only");
        }
    }

    private static string ConvertToString(PopupButtons button)
    {
        switch (button)
        {
            case PopupButtons.Ok:
                return Texts.ButtonOkText;

            case PopupButtons.Cancel:
                return Texts.ButtonCancelText;

            case PopupButtons.Exit:
                return Texts.ButtonExitText;

            default:
                throw new NotSupportedException("Unknown button");
        }
    }

    private class ButtonActions
    {
        public ButtonActions(PopupButtons[] activeButtons)
        {
            List<PopupButtons> buttons = activeButtons.ToList();
            bool isInitialzied = TryFindCancelButton(buttons, PopupButtons.Cancel);
            if (isInitialzied)
            {
                return;
            }

            isInitialzied = TryFindCancelButton(buttons, PopupButtons.Exit);
            if (isInitialzied)
            {
                return;
            }

            AcceptAction = buttons[0];
            CancelAction = buttons[1];
        }

        private bool TryFindCancelButton(List<PopupButtons> buttons, PopupButtons assumedCancelButton)
        {
            if (buttons.Contains(assumedCancelButton))
            {
                CancelAction = assumedCancelButton;
                buttons.Remove(assumedCancelButton);
                AcceptAction = buttons[0];
                return true;
            }

            return false;
        }

        public PopupButtons CancelAction { get; private set; }

        public PopupButtons AcceptAction { get; private set; }
    }
}