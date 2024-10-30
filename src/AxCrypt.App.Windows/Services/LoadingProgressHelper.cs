using AxCrypt.App.Components.Models;

namespace AxCrypt.App.Windows.Services;

public class LoadingProgressHelper
{
    public static async Task<T> ExecuteLoadingProgress<T>(Func<Task<T>> func, IProgress<LoadingModel> progress = null)
    {
        try
        {
            progress?.Report(new LoadingModel { IsLoading = true });
            return await func();
        }
        finally
        {
            progress?.Report(new LoadingModel { IsLoading = false });
        }
    }
}
