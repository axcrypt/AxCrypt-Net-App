using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Api.Model;
using AxCrypt.Core.Crypto;
using AxCrypt.App.Components.Models;
using AxCrypt.Content;
using AxCrypt.Core.Runtime;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Components.Services;

public class FileShareService : ViewModelBase, IDisposable
{
    private LogOnIdentity _identity;
    private FileOperationViewModel _fileOperationViewModel;
    private IEnumerable<string> _shareKeyFileNameList;

    private IEnumerable<ShareKeyUser> ShareKeyUserList { get; set; } = new List<ShareKeyUser>();

    public IEnumerable<string> SelectedFilesOrFolders
    {
        get { return GetProperty<IEnumerable<string>>(nameof(SelectedFilesOrFolders)); }
        set { SetProperty(nameof(SelectedFilesOrFolders), value); }
    }

    private SharingListViewModel _viewModel;

    public SharingListViewModel ViewModel
    {
        get
        {
            return _viewModel;
        }
        set
        {
            _viewModel = value;
        }
    }

    public void SetSelectedFilesOrFolders(IEnumerable<string> filesOrFoldersPath, SharingListViewModel sharingListViewModel)
    {
        SelectedFilesOrFolders = filesOrFoldersPath;
        _viewModel = sharingListViewModel;
        _viewModel.BindPropertyChanged<IEnumerable<UserPublicKey>>(nameof(SharingListViewModel.SharedWith), (aks) =>
        {
            ShareKeyUserList = aks.Distinct(UserPublicKey.EmailComparer).ToArray().Select(user =>
            {
                if (user != null && !string.IsNullOrEmpty(user.GroupName))
                {
                    return new ShareKeyUser(user.Email, user.GroupName);
                }

                return new ShareKeyUser(user.Email, AccountStatus.Verified);
            }).ToList();
        });

        ShareKeyUserList = _viewModel.SharedWith.Select(user => new ShareKeyUser(user.Email, AccountStatus.Verified)).ToList();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}