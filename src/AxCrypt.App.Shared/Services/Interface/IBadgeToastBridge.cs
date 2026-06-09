using System.Collections.Generic;

namespace AxCrypt.App.Shared.Services.Interface;

/// <summary>
/// Shared → Desktop bridge for publishing batch results to BatchOperationToast.
/// Desktop registers the concrete implementation; callers no-op if it's absent.
/// </summary>
public interface IBatchToastBridge
{
    /// <param name="operationName">Past-tense verb, e.g. "Encrypted".</param>
    /// <param name="succeededCount">Items processed without error.</param>
    /// <param name="failures">Optional (path, reason) list for failed items.</param>
    void PublishResult(
        string operationName,
        int succeededCount,
        IEnumerable<(string FilePath, string Reason)>? failures = null);
}