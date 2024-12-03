using System.Threading.Tasks;

namespace AxCrypt.Core.UI
{
    public interface IDataItemSelection
    {
        Task<bool> HandleSelection(FileSelectionEventArgs e);
    }
}