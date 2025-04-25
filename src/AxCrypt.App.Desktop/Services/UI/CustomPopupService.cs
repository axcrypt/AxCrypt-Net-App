using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.UI;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AxCrypt.App.Desktop.Services;

public class CustomPopupService : IPopup
{
    private static readonly PopupButtons[] possibleButtons = new PopupButtons[]
    {
        PopupButtons.Ok,
        PopupButtons.Cancel,
        PopupButtons.Exit,
    };

    public event Action<bool> OnPopupVisibilityChanged;

    private bool isVisible;

    public bool IsVisible
    {
        get => isVisible;
        set
        {
            isVisible = value;
            OnPopupVisibilityChanged?.Invoke(isVisible);
        }
    }
    public string? Title { get; set; }

    public string? Message { get; set; }

    public string[]? Buttons { get; set; }

    public DoNotShowAgainOptions? DontShowAgainOptions { get; set; }

    private void ShowPopup(PopupButtons[] buttons, string title, string message, DoNotShowAgainOptions doNotShowAgainOption )
    {
        Title = title;
        Message = message;
        //Buttons = buttons;
        DontShowAgainOptions = doNotShowAgainOption;
        IsVisible = true;
    }

    public void HidePopup()
    {
        Title = "";
        Message = "";
        //Buttons = buttons;
        DontShowAgainOptions = DoNotShowAgainOptions.None;

        IsVisible = false;
    }

    public Task<PopupButtons> ShowAsync(PopupButtons buttons, string title, string message)
    {
        return ShowAsync(buttons, title, message, DoNotShowAgainOptions.None);
    }

    public async Task<PopupButtons> ShowAsync(PopupButtons buttons, string title, string message, DoNotShowAgainOptions doNotShowAgainOption, string doNotShowAgainCustomText)
    {
        PopupButtons[] activeButtons = possibleButtons.Where(b => buttons.HasFlag(b)).ToArray();

        switch (activeButtons.Length)
        {
            case 1:
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    GlobalDialogViewModel upgradeVersionViewModel = new GlobalDialogViewModel();
                    await upgradeVersionViewModel.ShowVersionDialog(activeButtons[0], title, message, doNotShowAgainOption);
                    //await Application.Current.MainPage.DisplayAlert(title, message, ConvertToString(activeButtons[0]));
                });

                ShowPopup(activeButtons, title, message, doNotShowAgainOption);
                return activeButtons[0];

            case 2:
                ButtonActions actions = new ButtonActions(activeButtons);
                string leftButton = ConvertToString(actions.AcceptAction);
                string rightButton = ConvertToString(actions.CancelAction);

                bool isAccepted = false;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    GlobalDialogViewModel upgradeVersionViewModel = new GlobalDialogViewModel();
                    await upgradeVersionViewModel.ShowVersionDialog(activeButtons[0], title, message, doNotShowAgainOption);
                    isAccepted = upgradeVersionViewModel.LogOnViewModel.PageResult == Shared.Utility.DialogResult.OK;
                    //isAccepted = await Application.Current.MainPage.DisplayAlert(title, message, leftButton, rightButton);
                });

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
        switch (buttons.Length)
        {
            case 1:
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert(title, message, buttons[0]);
                });
                return buttons[0];

            case 2:
                string leftButton = buttons[0];
                string rightButton = buttons[1];
                bool isAccepted = false;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    isAccepted = await Application.Current.MainPage.DisplayAlert(title, message, leftButton, rightButton);
                });

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
