using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AxCrypt.App.Shared.Services;

/// <summary>
/// One place every unhandled exception in the app ends up. UI hosts a
/// global popup that subscribes to <see cref="OnErrorRaised"/>; the
/// platform layer (App.xaml.cs / MAUI handlers) reports exceptions here
/// instead of swallowing them silently.
///
/// Singleton — the same instance is shared between platform handlers
/// (which raise) and Razor components (which display).
///
/// Thread-safe: <see cref="Report(Exception, string?)"/> can be called
/// from any thread including UnobservedTaskException background threads.
/// The OnErrorRaised event delivers on the calling thread, so subscribers
/// must marshal to the UI dispatcher themselves (Blazor: InvokeAsync).
/// </summary>
public class ErrorReportService
{
    /// <summary>Latest reported error, populated by <see cref="Report"/>. </summary>
    public ReportedError? Current { get; private set; }

    /// <summary>True once the consumer hasn't yet dismissed the popup.</summary>
    public bool IsVisible { get; private set; }

    /// <summary>
    /// Raised every time an error is reported. Subscribers should set
    /// their own visible flag from <see cref="IsVisible"/> and StateHasChanged().
    /// </summary>
    public event Action<ReportedError>? OnErrorRaised;

    /// <summary>Raised when the user dismisses the popup.</summary>
    public event Action? OnDismissed;

    private readonly Lock _gate = new();

    /// <summary>Track recent errors so we don't fire 20 identical popups when a
    /// background loop keeps throwing the same exception.</summary>
    private string _lastSignature = string.Empty;
    private DateTime _lastSignatureAt = DateTime.MinValue;

    /// <summary>Window inside which an identical error is treated as a dup.</summary>
    private static readonly TimeSpan DedupWindow = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Report an exception. Safe to call from any thread.
    /// <paramref name="context"/> is a short label describing what was being
    /// attempted (e.g. "Encrypt file", "Sync vault") — appended to the
    /// message and stored on <see cref="ReportedError.Context"/>.
    /// </summary>
    public void Report(Exception? ex, string? context = null)
    {
        if (ex == null)
        {
            return;
        }

        // Drill to the most specific cause — AggregateException, TIE etc.
        // typically wrap the interesting one. Keep the outermost stack
        // separately so it isn't lost.
        Exception root = Unwrap(ex);
        string message = string.IsNullOrWhiteSpace(root.Message)
            ? root.GetType().Name
            : root.Message;

        Report(
            new ReportedError(
                Title: context ?? "Something went wrong",
                Message: message,
                Detail: BuildDetail(ex),
                Exception: ex,
                Context: context,
                Timestamp: DateTime.UtcNow));
    }

    /// <summary>Report a plain text error (no exception). Useful for
    /// surfaces like a status callback that hands back an error string.</summary>
    public void Report(string title, string message, string? detail = null)
    {
        Report(new ReportedError(
            Title: title,
            Message: message,
            Detail: detail ?? string.Empty,
            Exception: null,
            Context: title,
            Timestamp: DateTime.UtcNow));
    }

    private void Report(ReportedError error)
    {
        // Dedup — same signature within a few seconds collapses into the
        // existing popup rather than firing a fresh one. Prevents a tight
        // background-loop failure from spamming the UI.
        string signature = error.Title + "|" + error.Message;
        lock (_gate)
        {
            DateTime now = DateTime.UtcNow;
            if (signature == _lastSignature && now - _lastSignatureAt < DedupWindow)
            {
                return;
            }
            _lastSignature = signature;
            _lastSignatureAt = now;

            Current = error;
            IsVisible = true;
        }

        try
        {
            OnErrorRaised?.Invoke(error);
        }
        catch
        {
            // A faulty subscriber must not crash the error-reporting path.
        }
    }

    /// <summary>Dismiss the popup. Idempotent.</summary>
    public void Dismiss()
    {
        lock (_gate)
        {
            if (!IsVisible)
            {
                return;
            }
            IsVisible = false;
        }
        try
        {
            OnDismissed?.Invoke();
        }
        catch
        {
            // ignore — same reasoning as above
        }
    }

    private static Exception Unwrap(Exception ex)
    {
        if (ex is AggregateException agg && agg.InnerExceptions.Count > 0)
        {
            return Unwrap(agg.InnerExceptions[0]);
        }
        return ex.InnerException != null ? Unwrap(ex.InnerException) : ex;
    }

    /// <summary>
    /// Build a developer-friendly detail block. The popup hides this behind
    /// a "Show details" toggle and offers a copy-to-clipboard button.
    /// </summary>
    private static string BuildDetail(Exception ex)
    {
        var lines = new List<string>();
        Exception? current = ex;
        int depth = 0;
        while (current != null && depth < 6)
        {
            lines.Add($"[{depth}] {current.GetType().FullName}: {current.Message}");
            if (!string.IsNullOrWhiteSpace(current.StackTrace))
            {
                lines.Add(current.StackTrace);
            }
            current = current.InnerException;
            depth++;
        }
        return string.Join("\n", lines);
    }
}

/// <summary>Immutable snapshot of one reported error.</summary>
public record ReportedError(
    string Title,
    string Message,
    string Detail,
    Exception? Exception,
    string? Context,
    DateTime Timestamp);
