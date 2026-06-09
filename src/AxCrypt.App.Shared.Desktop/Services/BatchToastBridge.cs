using AxCrypt.App.Shared.Services.Interface;
using System.Collections.Generic;
using System.Linq;

namespace AxCrypt.App.Shared.Desktop.Services;

/// <summary>
/// Forwards Shared-layer batch results to BatchOperationToast via
/// <see cref="BatchFileOperationService.PublishExternalResult"/>.
/// </summary>
public class BatchToastBridge : IBatchToastBridge
{
    private readonly BatchFileOperationService _batchService;

    public BatchToastBridge(BatchFileOperationService batchService) => _batchService = batchService;

    public void PublishResult(
        string operationName,
        int succeededCount,
        IEnumerable<(string FilePath, string Reason)>? failures = null)
    {
        IEnumerable<BatchFileError>? mapped = failures?
            .Select(f => new BatchFileError(f.FilePath, f.Reason, null));

        _batchService.PublishExternalResult(operationName, succeededCount, mapped);
    }
}