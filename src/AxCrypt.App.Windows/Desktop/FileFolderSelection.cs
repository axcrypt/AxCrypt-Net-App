using AxCrypt.Content;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using Windows.Storage.Pickers;
using Windows.Storage;

using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.Abstractions;

namespace AxCrypt.App.Windows.Desktop;

public class FileFolderSelection : IDataItemSelection
{
    //private IWin32Window _owner;

    //public FileFolderSelection(IWin32Window owner)
    //{
    //    _owner = owner;
    //}
    public FileFolderSelection()
    {

    }

    public Task HandleSelection(FileSelectionEventArgs e)
    {
        if (e == null)
        {
            throw new ArgumentNullException(nameof(e));
        }

        try
        {
            New<IMainUI>().DisableUI();
            HandleSelectionInternal(e);
        }
        finally
        {
            New<IMainUI>().RestoreUI();
        }
        return Constant.CompletedTask;
    }

    private void HandleSelectionInternal(FileSelectionEventArgs e)
    {
        switch (e.FileSelectionType)
        {
            case FileSelectionType.SaveAsEncrypted:
            case FileSelectionType.SaveAsDecrypted:
                HandleSaveAsFileSelection(e);
                break;

            case FileSelectionType.WipeConfirm:
                HandleWipeConfirm(e);
                break;

            case FileSelectionType.Folder:
                HandleFolderSelection(e);
                break;

            default:
                HandleOpenFileSelection(e);
                break;
        }
    }

    private async void HandleFolderSelection(FileSelectionEventArgs e)
    {
        FolderPicker fldpik = new FolderPicker();
        fldpik.SettingsIdentifier = Texts.UpgradeLegacyFilesMenuToolTip;

        fldpik.SuggestedStartLocation = PickerLocationId.ComputerFolder;
        //fbd.User = _owner;

        //folderPicker.FileTypeFilter.Add("*");
        //folderPicker.ViewMode = PickerViewMode.Thumbnail;

        nint hwnd = ((MauiWinUIWindow)MauiWinUIApplication.Current.Application.Windows[0].Handler.PlatformView).WindowHandle;
        WinRT.Interop.InitializeWithWindow.Initialize(fldpik, hwnd);

        StorageFolder folders = await fldpik.PickSingleFolderAsync();
        e.SelectedFiles.Clear();
        if (folders == null || string.IsNullOrEmpty(folders.Path))
        {
            e.Cancel = true;
            return;
        }

        e.SelectedFiles.Add(folders?.Path);
    }

    private static void HandleWipeConfirm(FileSelectionEventArgs e)
    {
        //using (ConfirmWipeDialog cwd = new ConfirmWipeDialog())
        //{
        //    cwd.FileNameLabel.Text = Path.GetFileName(e.SelectedFiles[0]);
        //    e.Skip = false;
        //    DialogResult confirmResult = cwd.ShowDialog();
        //    e.ConfirmAll = cwd._confirmAllCheckBox.Checked;
        //    e.Skip = confirmResult == DialogResult.No;
        //    e.Cancel = confirmResult == DialogResult.Cancel;
        //}
    }

    private static async void HandleOpenFileSelection(FileSelectionEventArgs e)
    {
        if (e.SelectedFiles != null && e.SelectedFiles.Count > 0 && !String.IsNullOrEmpty(e.SelectedFiles[0]))
        {
            IDataContainer initialFolder = New<IDataContainer>(e.SelectedFiles[0]);
            if (initialFolder.IsAvailable)
            {
                //ofd.InitialDirectory = initialFolder.FullName;
            }
        }

        PickOptions pickOptions = new PickOptions();
        string defaultExt = New<IRuntimeEnvironment>().AxCryptExtension;
        string filterPattern = "";
        bool isMultiSelect = false;

        switch (e.FileSelectionType)
        {
            case FileSelectionType.Decrypt:
                pickOptions.PickerTitle = Texts.DecryptFileOpenDialogTitle;
                filterPattern = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + defaultExt, Texts.FileFilterFileTypeAxCryptFiles, Texts.FileFilterFileTypeAllFiles);
                isMultiSelect = true;
                break;

            case FileSelectionType.Rename:
                pickOptions.PickerTitle = Texts.AnonymousRenameSelectFilesDialogTitle;
                filterPattern = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + defaultExt, Texts.FileFilterFileTypeAxCryptFiles, Texts.FileFilterFileTypeAllFiles);
                isMultiSelect = true;
                break;

            case FileSelectionType.Encrypt:
                pickOptions.PickerTitle = Texts.EncryptFileOpenDialogTitle;
                isMultiSelect = true;
                filterPattern = " ";
                break;

            case FileSelectionType.Open:
                pickOptions.PickerTitle = Texts.OpenEncryptedFileOpenDialogTitle;
                isMultiSelect = true;
                filterPattern = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + defaultExt, Texts.FileFilterFileTypeAxCryptFiles, Texts.FileFilterFileTypeAllFiles);
                //consider FileOpenPicker for read only selections
                break;

            case FileSelectionType.Wipe:
                pickOptions.PickerTitle = Texts.WipeFileSelectFileDialogTitle;
                isMultiSelect = true;
                filterPattern = " ";
                break;

            case FileSelectionType.ImportPublicKeys:
                pickOptions.PickerTitle = Texts.ImportPublicKeysFileSelectionTitle;
                isMultiSelect = true;
                filterPattern = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + ".txt", Texts.FileFilterFileTypePublicSharingKeyFiles, Texts.FileFilterFileTypeAllFiles);
                break;

            case FileSelectionType.ImportPrivateKeys:
                pickOptions.PickerTitle = Texts.ImportPrivateKeysFileSelectionTitle;
                isMultiSelect = false;
                filterPattern = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + defaultExt, Texts.FileFilterFileTypeAxCryptIdFiles, Texts.FileFilterFileTypeAllFiles);
                break;

            case FileSelectionType.KeySharing:
                pickOptions.PickerTitle = Texts.ShareKeysFileOpenDialogTitle;
                isMultiSelect = true;
                filterPattern = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + defaultExt, Texts.FileFilterFileTypeAxCryptFiles, Texts.FileFilterFileTypeAllFiles);
                break;

            case FileSelectionType.KeySharingEncrypt:
                pickOptions.PickerTitle = Texts.ShareKeysFileOpenDialogTitle;
                isMultiSelect = true;
                filterPattern = " ";
                break;

            default:
                break;
        }

        IDictionary<DevicePlatform, IEnumerable<string>> filterValuePairs = new Dictionary<DevicePlatform, IEnumerable<string>>()
        {
            { DevicePlatform.WinUI, filterPattern.Split(" ") }
        };
        pickOptions.FileTypes = new FilePickerFileType(filterValuePairs);

        IEnumerable<FileResult> ofd;
        if (isMultiSelect)
        {
            ofd = await FilePicker.PickMultipleAsync(pickOptions);
        }
        else
        {
            FileResult? fileResult = await FilePicker.PickAsync(pickOptions);
            ofd = fileResult == null ? new List<FileResult>() : new List<FileResult> { fileResult };
        }

        e.SelectedFiles?.Clear();
        if (ofd == null || !ofd.Any())
        {
            e.Cancel = true;
            return;
        }

        foreach (string fileName in ofd.Select(file => file.FullPath))
        {
            e.SelectedFiles?.Add(fileName);
        }
    }

    private static async void HandleSaveAsFileSelection(FileSelectionEventArgs e)
    {
        FileSavePicker fsp = new FileSavePicker();
        string filterPattern = "";

        switch (e.FileSelectionType)
        {
            case FileSelectionType.SaveAsEncrypted:
                fsp.SettingsIdentifier = Texts.EncryptFileSaveAsDialogTitle;
                fsp.DefaultFileExtension = OS.Current.AxCryptExtension;
                filterPattern = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + OS.Current.AxCryptExtension, Texts.FileFilterFileTypeAxCryptFiles, Texts.FileFilterFileTypeAllFiles);
                break;

            case FileSelectionType.SaveAsDecrypted:
                fsp.SettingsIdentifier = Texts.DecryptedSaveAsFileDialogTitle;
                string extension = Path.GetExtension(e.SelectedFiles[0]);
                fsp.DefaultFileExtension = extension;
                filterPattern = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + extension, Texts.FileFilterFileTypeFiles, Texts.FileFilterFileTypeAllFiles);
                break;
        }

        fsp.SuggestedFileName = Path.GetFileName(e.SelectedFiles[0]);
        fsp.SuggestedStartLocation = PickerLocationId.Unspecified;
        //Path.GetDirectoryName(e.SelectedFiles[0]);

        foreach (string filPat in filterPattern.Split(' '))
        {
            string[] filterPaths = filPat.Split('|');
            if (filterPaths.Length == 1)
                fsp.FileTypeChoices.Add("", new List<string> { filterPaths[0] });
            if (filterPaths.Length == 2)
                fsp.FileTypeChoices.Add(filterPaths[0], new List<string> { filterPaths[1] });
        }

        nint hwnd = ((MauiWinUIWindow)Application.Current.Windows[0].Handler.PlatformView).WindowHandle;
        WinRT.Interop.InitializeWithWindow.Initialize(fsp, hwnd);
        StorageFile storageFile = await fsp.PickSaveFileAsync();
        if (storageFile == null)
        {
            e.Cancel = true;
            return;
        }

        e.SelectedFiles[0] = storageFile.Path;
    }
}
