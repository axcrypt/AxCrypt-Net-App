using AxCrypt.App.Shared.Desktop.Code;
using System;

namespace AxCrypt.App.Shared.Desktop.Services.Interface;

public interface ITrayService
{
    void Initialize();

    Action<ContextMenuItem> ClickHandler { get; set; }

    bool Created { get; }

    void Hide();

    void EnsureVisible();

    void Remove();

    void Dispose();
}