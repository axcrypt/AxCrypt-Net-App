// Per-component async re-entrancy guard. Prevents double-click from launching concurrent file operations.
// Usage: private readonly OperationGuard _guard = new();
// Handler: await _guard.RunAsync(() => vm.DoSomethingAsync(), () => InvokeAsync(StateHasChanged));
// Template: <button disabled="@_guard.IsProcessing" @onclick="OnAction">

using System;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.Services;

public sealed class OperationGuard
{
    private bool _isProcessing;

    public bool IsProcessing => _isProcessing;

    // Drops the call silently if already processing; calls onStateChanged before/after to refresh UI.
    public async Task RunAsync(Func<Task> operation, Func<Task> onStateChanged)
    {
        if (_isProcessing) return;
        _isProcessing = true;
        await onStateChanged();
        try
        {
            await operation();
        }
        finally
        {
            _isProcessing = false;
            await onStateChanged();
        }
    }
}
