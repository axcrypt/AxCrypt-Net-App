using System.Collections.Concurrent;
using AxCrypt.Abstractions;
using AxCrypt.App.Shared.FileOperations.Vault;
using AxCrypt.Common;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.Portable;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.FileOperations.IO;

/// <summary>
/// Performs file operations with controlled degree of parallelism.
/// </summary>
public class CustomParallelFileOperation
{
    private readonly int _maxDegreeOfParallelism;

    public CustomParallelFileOperation(int maxDegreeOfParallelism = 4)
    {
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    public virtual async Task DoFilesAsync<TDataItem>(
        IEnumerable<TDataItem> files,
        Func<TDataItem, IProgressContext, Task<FileOperationContext>> work,
        Func<FileOperationContext, Task> allComplete
    )
        where TDataItem : IVaultDataStore
    {
        Func<TDataItem, IProgressContext, Task<FileOperationContext>> singleFileOperation =
            async (file, progress) =>
            {
                progress.Display = file.File.Name;
                return await work(file, progress);
            };
        await InvokeAsync(files, singleFileOperation, async (status) =>
        {
            if (status.ErrorStatus == ErrorStatus.Success)
            {
                status.Totals.ShowNotification();
            }
            await allComplete(status);
        });
    }

    private async Task InvokeAsync<T>(IEnumerable<T> files, Func<T, IProgressContext, Task<FileOperationContext>> workAsync, Func<FileOperationContext, Task> allCompleteAsync)
    {
        WorkerGroupProgressContext groupProgress = new WorkerGroupProgressContext(new CancelProgressContext(new ProgressContext()), New<ISingleThread>());

        await New<IProgressBackground>()
            .WorkAsync(
                nameof(DoFilesAsync),
                async (IProgressContext progress) =>
                {
                    progress.NotifyLevelStart();

                    using CancellationTokenSource cts = new CancellationTokenSource(); 
                    progress.OnCancelled += (s, e) => cts.Cancel();

                    ConcurrentBag<FileOperationContext> results = new ConcurrentBag<FileOperationContext>();
                    try
                    {
                        await Parallel.ForEachAsync(
                            files,
                            new ParallelOptions
                            {
                                MaxDegreeOfParallelism = _maxDegreeOfParallelism,
                                CancellationToken = cts.Token
                            },
                            async (file, token) =>
                            {
                                try
                                {
                                    FileOperationContext context = await workAsync(file, progress);
                                    results.Add(context);

                                    if (context.ErrorStatus == ErrorStatus.Success)
                                    {
                                        progress.Totals.AddFileCount(1);
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    results.Add(new FileOperationContext(file.ToString(), ErrorStatus.Canceled));
                                }
                                catch (AxCryptException ace)
                                {
                                    New<IReport>().Exception(ace);
                                    results.Add(new FileOperationContext(
                                        ace.DisplayContext.Default(file),
                                        ace.InnerException?.Message ?? ace.Message,
                                        ace.ErrorStatus));
                                }
                                catch (Exception ex)
                                {
                                    New<IReport>().Exception(ex);
                                    results.Add(new FileOperationContext(file.ToString(), ex.Message, ErrorStatus.Exception));
                                }
                            }
                        );
                    }
                    catch (OperationCanceledException)
                    {
                        // Global cancellation triggered
                        return new FileOperationContext("Operation canceled", ErrorStatus.Canceled);
                    }

                    progress.NotifyLevelFinished();

                    FileOperationContext finalResult =
                        results.FirstOrDefault(r => r.ErrorStatus != ErrorStatus.Success)
                        ?? new FileOperationContext(progress.Totals);

                    return finalResult;
                },
                async (FileOperationContext status) => await allCompleteAsync(status),
                groupProgress
            )
            .Free();
    }
}
