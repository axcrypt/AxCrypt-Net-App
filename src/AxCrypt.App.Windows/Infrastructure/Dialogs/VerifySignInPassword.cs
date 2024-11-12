using AxCrypt.Core.UI;

namespace AxCrypt.App.Windows.Infrastructure.Dialogs;

public class VerifySignInPassword : VerifySignInPasswordBase
{
    private Page _parent;

    public VerifySignInPassword(Page parent)
    {
        _parent = parent;
    }

    protected override bool VerifyDialog(string description)
    {
        return false;
        //trigger verify password dialog
        //return _parent.ShowVerifySignInPasswordDialog(description);
    }
}
