using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;

namespace AxCrypt.App.Shared.Desktop.Services;

/// <summary>
/// Runs an async per-file operation across a batch of files in a
/// fault-tolerant way. The key behavior change vs. passing the whole
/// list to <c>FileOperationViewModel.EncryptFiles / DecryptFiles / OpenFiles</c>
/// in one call is that we invoke the operation <b>one file at a time</b>
/// and wrap each call in try/catch. A single bad file (locked, permission
/// denied, password wrong, disk full, etc.) no longer aborts the whole
/// batch — every remaining file still gets processed, and the user gets
/// a summary with the failing files and their reasons.
/// </summary>
public class BatchFileOperationService
{
    private readonly IStatusAlertService? _statusAlertService;

    public BatchFileOperationService()
    {
        _statusAlertService = AxCServiceProviderExtension.StatusAlertService;
    }

    /// <summary>
    /// Latest batch result, exposed so a UI component (e.g. a toast/dialog)
    /// can render the success/failure summary.
    /// </summary>
    public BatchOperationResult? LastResult { get; private set; }

    /// <summary>
    /// Fires after every per-file step (success or failure). Useful for
    /// progress bars / live counters. <c>(completedCount, totalCount)</c>.
    /// </summary>
    public event Action<int, int>? OnProgress;

    /// <summary>Fires once the whole batch is done.</summary>
    public event Action<BatchOperationResult>? OnFinished;

    /// <summary>
    /// Run <paramref name="perFile"/> for every path in <paramref name="filePaths"/>.
    /// Errors raised by an individual file are captured and the loop
    /// keeps going. After the loop ends, the result is pushed to the
    /// status alert service (success summary or failure summary) and to
    /// <see cref="OnFinished"/> for any subscriber.
    /// </summary>
    /// <param name="filePaths">Files to process.</param>
    /// <param name="perFile">Async work for one file (e.g. call EncryptFiles with a single-item list).</param>
    /// <param name="operationName">Human-readable verb shown in the summary ("Encrypted", "Decrypted", "Opened", "Removed").</param>
    /// <param name="meteredFeature">
    /// When set, the count of files this batch processed successfully is
    /// reported to the entitlement provider once the batch finishes — this
    /// is how the free-tier FreePlanLimitBar moves the instant an encrypt
    /// batch completes. Leave null for operations that aren't metered
    /// (decrypt, open, remove, share).
    /// </param>
    public async Task<BatchOperationResult> RunAsync(
        IEnumerable<string>? filePaths,
        Func<string, Task> perFile,
        string operationName,
        FeatureKey? meteredFeature = null)
    {
        List<string> paths = filePaths?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>();
        BatchOperationResult result = new BatchOperationResult(operationName, paths.Count);

        for (int i = 0; i < paths.Count; i++)
        {
            string path = paths[i];
            try
            {
                await perFile(path);
                result.Succeeded.Add(path);
            }
            catch (Exception ex)
            {
                // Swallow — explicitly. The whole point of this service is
                // to keep iterating after a single file blows up.
                result.Failed.Add(new BatchFileError(path, FriendlyReason(ex), ex));
            }
            OnProgress?.Invoke(i + 1, paths.Count);
        }

        LastResult = result;
        PublishSummary(result);
        OnFinished?.Invoke(result);

        // Report metered usage (e.g. file encryption) so the free-tier
        // usage bar updates as soon as the batch finishes. Fire-and-forget:
        // the provider bumps its snapshot + notifies the UI synchronously,
        // then syncs to the server in the background — we never want usage
        // metering to add latency to (or fail) the file operation itself.
        _ = UpdateMeteredUsageAsync(meteredFeature, result);

        return result;
    }

    /// <summary>
    /// When a batch maps to a metered free-tier feature (currently file
    /// encryption), report the count of files it processed successfully to
    /// <see cref="IFeatureUsageProvider"/>. The provider raises its own
    /// change event, so the FreePlanLimitBar re-renders with the new count.
    /// </summary>
    private async Task UpdateMeteredUsageAsync(FeatureKey? feature, BatchOperationResult result)
    {
        if (feature == null || result.Succeeded.Count == 0)
        {
            return;
        }

        await RecordMeteredUsageAsync(feature, result.Succeeded.Count);
    }

    public async Task RecordMeteredUsageAsync(FeatureKey? feature, int count = 1)
    {
        try
        {
            IFeatureUsageProvider? usage = AxCServiceProviderExtension.GetService<IFeatureUsageProvider>();
            if (usage != null)
            {
                await usage.RecordUsageAsync(feature!.Value, count);
            }
        }
        catch
        {
            // Usage metering must never break the file operation itself.
        }
    }

    private void PublishSummary(BatchOperationResult result)
    {
        if (_statusAlertService == null || result.Total == 0)
        {
            return;
        }

        // Light-weight: feed the existing toast service. The richer
        // BatchOperationToast component (if mounted) also subscribes
        // to OnFinished for the detailed breakdown.
        string summary;
        if (result.HasFailures && result.HasSucceeded)
        {
            summary = $"{result.OperationName} {result.Succeeded.Count}/{result.Total} files. {result.Failed.Count} failed.";
            _statusAlertService.Error(summary);
        }
        else if (result.HasFailures)
        {
            summary = $"{result.OperationName} failed for all {result.Failed.Count} file(s).";
            _statusAlertService.Error(summary);
        }
        else
        {
            summary = $"{result.OperationName} {result.Succeeded.Count} file(s) successfully.";
            _statusAlertService.Success(summary);
        }
    }

    /// <summary>
    /// Map a raw exception to a short, user-readable reason. Falls back to
    /// <c>ex.Message</c> if no specific mapping applies. We deliberately
    /// do NOT include stack traces or assembly-qualified type names —
    /// those belong in logs, not the UI.
    /// </summary>
    private static string FriendlyReason(Exception ex)
    {
        switch (ex)
        {
            case UnauthorizedAccessException:
                return "Permission denied";

            case FileNotFoundException:
                return "File not found";

            case DirectoryNotFoundException:
                return "Folder not found";

            case IOException ioex when ioex.Message.Contains("being used"):
                return "File is open in another program";

            case IOException ioex:
                return ioex.Message;

            case OperationCanceledException:
                return "Cancelled";

            default:
                return string.IsNullOrWhiteSpace(ex.Message) ? ex.GetType().Name : ex.Message;
        }
    }
}

/// <summary>One file's outcome within a batch.</summary>
public sealed class BatchFileError
{
    public string FilePath { get; }
    public string FileName => System.IO.Path.GetFileName(FilePath);
    public string Reason { get; }
    public Exception? Exception { get; }

    public BatchFileError(string filePath, string reason, Exception? exception)
    {
        FilePath = filePath;
        Reason = reason;
        Exception = exception;
    }
}

/// <summary>Aggregate result of a batch run.</summary>
public sealed class BatchOperationResult
{
    /// <summary>Verb form for the summary, e.g. "Encrypted".</summary>
    public string OperationName { get; }

    public int Total { get; }
    public List<string> Succeeded { get; } = new();
    public List<BatchFileError> Failed { get; } = new();
    public bool HasSucceeded => Succeeded.Count > 0;
    public bool HasFailures => Failed.Count > 0;
    public int ProcessedCount => Succeeded.Count + Failed.Count;

    public BatchOperationResult(string name, int total)
    {
        OperationName = name;
        Total = total;
    }
}