using AxCrypt.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AxCrypt.Core.IO
{
    public class FileFilter
    {
        private readonly List<Regex> pathFilters;
        private readonly List<string> forbiddenFolderFilters;
        private readonly List<string> forbiddenExactFolders;

        public FileFilter()
        {
            pathFilters = new List<Regex>();
            forbiddenFolderFilters = new List<string>();
            forbiddenExactFolders = new List<string>();
        }

        public void AddPlatformIndependent()
        {
            AddUnencryptableExtension("cloudf");
            AddUnencryptableExtension("cloud");
            AddUnencryptableExtension("lnk");
            AddUnencryptableExtension("website");
            AddUnencryptableExtension("url");
            AddUnencryptableExtension("pif");
            AddUnencryptableExtension("gsheet");
            AddUnencryptableExtension("gdoc");
            AddUnencryptableExtension("gslides");
            AddUnencryptableExtension("gdraw");
            AddUnencryptableExtension("gtable");
            AddUnencryptableExtension("gform");
            AddUnencryptableExtension("ds_store");
            AddUnencryptableExtension("sys");
        }

        public bool IsEncryptable(IDataItem fileInfo)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException("fileInfo");
            }

            foreach (Regex filter in pathFilters)
            {
                if (filter.IsMatch(fileInfo.FullName))
                {
                    return false;
                }
            }
            return !fileInfo.IsEncrypted();
        }

        public bool IsForbiddenFolder(string folder)
        {
            if (folder == null)
            {
                throw new ArgumentNullException("folder");
            }

            if (folder == Resolve.UserSettings.TemporaryFilePath)
            {
                return true;
            }

            string normalizedFolder = folder.NormalizeFolderPath().ToLower();

            // Exact-match roots: the folder itself is forbidden, but its subfolders are allowed (e.g. the user profile root).
            if (forbiddenExactFolders.Contains(normalizedFolder))
            {
                return true;
            }

            foreach (string filter in forbiddenFolderFilters)
            {
                if (normalizedFolder.StartsWith(filter))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Forbids exactly this folder without forbidding its subfolders.</summary>
        public bool AddForbiddenFolderExact(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            forbiddenExactFolders.Add(path.NormalizeFolderPath().ToLower());
            return true;
        }

        public bool AddUnencryptable(Regex regex)
        {
            if (regex == null)
            {
                throw new ArgumentNullException(nameof(regex));
            }
            pathFilters.Add(regex);
            return true;
        }

        public bool AddUnencryptableExtension(string extension)
        {
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }
            pathFilters.Add(new Regex(@".*\." + extension + "$"));
            return true;
        }

        public bool AddForbiddenFolderFilters(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            forbiddenFolderFilters.Add(path.NormalizeFolderPath().ToLower());
            return true;
        }
    }
}