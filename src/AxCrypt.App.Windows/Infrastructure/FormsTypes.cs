using AxCrypt.Abstractions;
using AxCrypt.App.Windows.Desktop;
using AxCrypt.App.Windows.Infrastructure.Dialogs;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;

namespace AxCrypt.App.Windows.Infrastructure;

public static class FormsTypes
{
    public static void Register(App app)
    {
        TypeMap.Register.Singleton<IPopup>(() => new PopupService());
        //TypeMap.Register.Singleton<IPopup>(() => new Popup(parent));
        TypeMap.Register.Singleton<IVerifySignInPassword>(() => new VerifySignInPassword(app.MainPage));
        TypeMap.Register.Singleton<IMainUI>(() => new MainUI(app.MainPage));
    }
}
