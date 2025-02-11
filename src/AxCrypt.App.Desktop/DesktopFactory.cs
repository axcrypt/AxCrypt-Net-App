using AxCrypt.Abstractions;
using AxCrypt.App.Desktop.Implementation;
using AxCrypt.Core;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using System;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop;

public static class DesktopFactory
{
    public static void RegisterTypeFactories()
    {
        TypeMap.Register.New<string, IFileWatcher>((path) => new FileWatcher(path, new DelayedAction(New<IDelayTimer>(), TimeSpan.FromMilliseconds(500))));
        TypeMap.Register.Singleton<ISettingsStore>(() => new SettingsStore(Resolve.WorkFolder.FileInfo.FileItemInfo("UserSettings.txt")));
    }
}
