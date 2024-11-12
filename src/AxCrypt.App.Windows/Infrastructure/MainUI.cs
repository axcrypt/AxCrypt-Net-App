using AxCrypt.Core.UI;

namespace AxCrypt.App.Windows.Infrastructure;

public class MainUI : IMainUI
{
    private Stack<bool> _states = new Stack<bool>();

    private Page _mainForm;

    public MainUI(Page mainForm)
    {
        _mainForm = mainForm;
    }

    public void DisableUI()
    {
        _states.Push(_mainForm.IsEnabled);
        _mainForm.IsEnabled = false;
    }

    public void RestoreUI()
    {
        _mainForm.IsEnabled = _states.Pop();
    }
}
