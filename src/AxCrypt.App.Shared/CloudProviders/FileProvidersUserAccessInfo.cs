using AxCrypt.Abstractions;
using AxCrypt.Core.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Providers
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class FileProvidersUserAccessInfo : IDisposable
    {
        public FileProvidersUserAccessInfo()
        {
            GoogleDriveAccessInfo = new List<GoogleDriveAccessInfo>();
            DropBoxAccessInfo = new List<DropBoxAccessInfo>();
            OneDriveAccessInfo = new List<OneDriveAccessInfo>();
            iCloudAccessInfo = new List<iCloudAccessInfo>();
        }

        [JsonProperty("google_drive")]
        public IEnumerable<GoogleDriveAccessInfo> GoogleDriveAccessInfo { get; set; }

        [JsonProperty("drop_box")]
        public IEnumerable<DropBoxAccessInfo> DropBoxAccessInfo { get; set; }

        [JsonProperty("one_drive")]
        public IEnumerable<OneDriveAccessInfo> OneDriveAccessInfo { get; set; }

        [JsonProperty("i_cloud")]
        public IEnumerable<iCloudAccessInfo> iCloudAccessInfo { get; set; }

        private static IDataStore _fileProvidersUserAccessInfoStore;
        private static FileProvidersUserAccessInfo _fileProvidersUserAccessInfo;

        public static FileProvidersUserAccessInfo Create(IDataStore dataStore)
        {
            _fileProvidersUserAccessInfoStore = dataStore;

            if (_fileProvidersUserAccessInfoStore == null || !_fileProvidersUserAccessInfoStore.IsAvailable)
            {
                return new FileProvidersUserAccessInfo();
            }

            using (New<FileLocker>().Acquire(_fileProvidersUserAccessInfoStore))
            {
                _fileProvidersUserAccessInfo = New<IStringSerializer>().Deserialize<FileProvidersUserAccessInfo>(_fileProvidersUserAccessInfoStore.OpenRead());
            }

            return _fileProvidersUserAccessInfo ?? new FileProvidersUserAccessInfo();
        }

        public void Add(GoogleDriveAccessInfo newDriveAccessInfo)
        {
            if (newDriveAccessInfo == null)
            {
                return;
            }

            IList<GoogleDriveAccessInfo> googleDriveAccessInfoList = new List<GoogleDriveAccessInfo>();
            if (_fileProvidersUserAccessInfo != null)
            {
                googleDriveAccessInfoList = _fileProvidersUserAccessInfo.GoogleDriveAccessInfo.ToList();
            }

            GoogleDriveAccessInfo oldDriveAccessInfo = googleDriveAccessInfoList.SingleOrDefault(gdf => gdf.RefreshToken == newDriveAccessInfo.RefreshToken);
            if (oldDriveAccessInfo == null)
            {
                googleDriveAccessInfoList.Add(newDriveAccessInfo);
                _fileProvidersUserAccessInfo = _fileProvidersUserAccessInfo ?? new FileProvidersUserAccessInfo();
                _fileProvidersUserAccessInfo.GoogleDriveAccessInfo = googleDriveAccessInfoList;
                Save(_fileProvidersUserAccessInfo);
                return;
            }

            if (oldDriveAccessInfo == newDriveAccessInfo)
            {
                return;
            }

            googleDriveAccessInfoList.Remove(oldDriveAccessInfo);
            googleDriveAccessInfoList.Add(newDriveAccessInfo);

            _fileProvidersUserAccessInfo.GoogleDriveAccessInfo = googleDriveAccessInfoList;
            Save(_fileProvidersUserAccessInfo);
        }

        public void Remove(GoogleDriveAccessInfo driveAccessInfo)
        {
            if (_fileProvidersUserAccessInfo == null)
            {
                return;
            }

            IList<GoogleDriveAccessInfo> googleDriveAccessInfoList = _fileProvidersUserAccessInfo.GoogleDriveAccessInfo.ToList();

            GoogleDriveAccessInfo gDriveAccessInfo = googleDriveAccessInfoList.SingleOrDefault(gdf => gdf.RefreshToken == driveAccessInfo.RefreshToken);
            if (gDriveAccessInfo == null)
            {
                return;
            }

            googleDriveAccessInfoList.Remove(gDriveAccessInfo);

            _fileProvidersUserAccessInfo.GoogleDriveAccessInfo = googleDriveAccessInfoList;
            Save(_fileProvidersUserAccessInfo);
        }

        public void Add(DropBoxAccessInfo newDropBoxAccessInfo)
        {
            if (newDropBoxAccessInfo == null)
            {
                return;
            }

            IList<DropBoxAccessInfo> dropBoxAccessInfoList = new List<DropBoxAccessInfo>();
            if (_fileProvidersUserAccessInfo != null)
            {
                dropBoxAccessInfoList = _fileProvidersUserAccessInfo.DropBoxAccessInfo.ToList();
            }

            DropBoxAccessInfo oldDriveAccessInfo = dropBoxAccessInfoList.SingleOrDefault(dbf => dbf.AccessToken == newDropBoxAccessInfo.AccessToken);// Use refresh token, after started using refresh token.
            if (oldDriveAccessInfo == null)
            {
                dropBoxAccessInfoList.Add(newDropBoxAccessInfo);
                _fileProvidersUserAccessInfo = _fileProvidersUserAccessInfo ?? new FileProvidersUserAccessInfo();
                _fileProvidersUserAccessInfo.DropBoxAccessInfo = dropBoxAccessInfoList;
                Save(_fileProvidersUserAccessInfo);
                return;
            }

            if (oldDriveAccessInfo == newDropBoxAccessInfo)
            {
                return;
            }

            dropBoxAccessInfoList.Remove(oldDriveAccessInfo);
            dropBoxAccessInfoList.Add(newDropBoxAccessInfo);

            _fileProvidersUserAccessInfo.DropBoxAccessInfo = dropBoxAccessInfoList;
            Save(_fileProvidersUserAccessInfo);
        }

        public void Remove(DropBoxAccessInfo driveAccessInfo)
        {
            if (_fileProvidersUserAccessInfo == null)
            {
                return;
            }

            IList<DropBoxAccessInfo> dropBoxAccessInfoList = _fileProvidersUserAccessInfo.DropBoxAccessInfo.ToList();

            DropBoxAccessInfo gDriveAccessInfo = dropBoxAccessInfoList.SingleOrDefault(dbi => dbi.RefreshToken == driveAccessInfo.RefreshToken);
            if (gDriveAccessInfo == null)
            {
                return;
            }

            dropBoxAccessInfoList.Remove(gDriveAccessInfo);

            _fileProvidersUserAccessInfo.DropBoxAccessInfo = dropBoxAccessInfoList;
            Save(_fileProvidersUserAccessInfo);
        }

        public void Add(OneDriveAccessInfo newOneDriveAccessInfo)
        {
            if (newOneDriveAccessInfo == null)
            {
                return;
            }

            IList<OneDriveAccessInfo> oneDriveAccessInfoList = new List<OneDriveAccessInfo>();
            if (_fileProvidersUserAccessInfo != null)
            {
                oneDriveAccessInfoList = _fileProvidersUserAccessInfo.OneDriveAccessInfo.ToList();
            }

            OneDriveAccessInfo oldDriveAccessInfo = oneDriveAccessInfoList.SingleOrDefault(dbf => dbf.AccessToken == newOneDriveAccessInfo.AccessToken);// Use refresh token, after started using refresh token.
            if (oldDriveAccessInfo == null)
            {
                oneDriveAccessInfoList.Add(newOneDriveAccessInfo);
                _fileProvidersUserAccessInfo = _fileProvidersUserAccessInfo ?? new FileProvidersUserAccessInfo();
                _fileProvidersUserAccessInfo.OneDriveAccessInfo = oneDriveAccessInfoList;
                Save(_fileProvidersUserAccessInfo);
                return;
            }

            if (oldDriveAccessInfo == newOneDriveAccessInfo)
            {
                return;
            }

            oneDriveAccessInfoList.Remove(oldDriveAccessInfo);
            oneDriveAccessInfoList.Add(newOneDriveAccessInfo);

            _fileProvidersUserAccessInfo.OneDriveAccessInfo = oneDriveAccessInfoList;
            Save(_fileProvidersUserAccessInfo);
        }

        public void Remove(OneDriveAccessInfo driveAccessInfo)
        {
            if (_fileProvidersUserAccessInfo == null)
            {
                return;
            }

            IList<OneDriveAccessInfo> oneDriveAccessInfoList = _fileProvidersUserAccessInfo.OneDriveAccessInfo.ToList();

            OneDriveAccessInfo gDriveAccessInfo = oneDriveAccessInfoList.SingleOrDefault(dbi => dbi.RefreshToken == driveAccessInfo.RefreshToken);
            if (gDriveAccessInfo == null)
            {
                return;
            }

            oneDriveAccessInfoList.Remove(gDriveAccessInfo);

            _fileProvidersUserAccessInfo.OneDriveAccessInfo = oneDriveAccessInfoList;
            Save(_fileProvidersUserAccessInfo);
        }

        public void Add(iCloudAccessInfo newDriveAccessInfo)
        {
            if (newDriveAccessInfo == null)
            {
                return;
            }

            IList<iCloudAccessInfo> iCloudAccessInfoList = new List<iCloudAccessInfo>();
            if (_fileProvidersUserAccessInfo != null)
            {
                iCloudAccessInfoList = _fileProvidersUserAccessInfo.iCloudAccessInfo.ToList();
            }

            iCloudAccessInfo? oldDriveAccessInfo = iCloudAccessInfoList.SingleOrDefault(gdf => gdf.RefreshToken == newDriveAccessInfo.RefreshToken);
            if (oldDriveAccessInfo == null)
            {
                iCloudAccessInfoList.Add(newDriveAccessInfo);
                _fileProvidersUserAccessInfo = _fileProvidersUserAccessInfo ?? new FileProvidersUserAccessInfo();
                _fileProvidersUserAccessInfo.iCloudAccessInfo = iCloudAccessInfoList;
                Save(_fileProvidersUserAccessInfo);
                return;
            }

            if (oldDriveAccessInfo == newDriveAccessInfo)
            {
                return;
            }

            iCloudAccessInfoList.Remove(oldDriveAccessInfo);
            iCloudAccessInfoList.Add(newDriveAccessInfo);

            _fileProvidersUserAccessInfo.iCloudAccessInfo = iCloudAccessInfoList;
            Save(_fileProvidersUserAccessInfo);
        }

        public void Remove(iCloudAccessInfo driveAccessInfo)
        {
            if (_fileProvidersUserAccessInfo == null)
            {
                return;
            }

            IList<iCloudAccessInfo> iCloudAccessInfoList = _fileProvidersUserAccessInfo.iCloudAccessInfo.ToList();

            iCloudAccessInfo? gDriveAccessInfo = iCloudAccessInfoList.SingleOrDefault(gdf => gdf.RefreshToken == driveAccessInfo.RefreshToken);
            if (gDriveAccessInfo == null)
            {
                return;
            }

            iCloudAccessInfoList.Remove(gDriveAccessInfo);

            _fileProvidersUserAccessInfo.iCloudAccessInfo = iCloudAccessInfoList;
            Save(_fileProvidersUserAccessInfo);
        }

        public void RemoveAll()
        {
            if (_fileProvidersUserAccessInfo == null)
            {
                return;
            }

            _fileProvidersUserAccessInfoStore.Delete();

            _fileProvidersUserAccessInfo = new FileProvidersUserAccessInfo();
            Save(_fileProvidersUserAccessInfo);
        }

        public void Save(FileProvidersUserAccessInfo userAccessInfo)
        {
            lock (_fileProvidersUserAccessInfoStore)
            {
                string currentJson = string.Empty;
                if (_fileProvidersUserAccessInfoStore.IsAvailable)
                {
                    using (StreamReader reader = new StreamReader(_fileProvidersUserAccessInfoStore.OpenRead(), Encoding.UTF8))
                    {
                        currentJson = reader.ReadToEnd();
                    }
                }

                string updatedJson = AxCrypt.Core.Resolve.Serializer.Serialize(userAccessInfo);
                if (currentJson == updatedJson)
                {
                    return;
                }

                using (StreamWriter writer = new StreamWriter(_fileProvidersUserAccessInfoStore.OpenWrite(), Encoding.UTF8))
                {
                    writer.Write(updatedJson);
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeInternal();
            }
        }

        private void DisposeInternal()
        {
            if (_fileProvidersUserAccessInfoStore != null)
            {
                _fileProvidersUserAccessInfoStore = null;
            }

            if (_fileProvidersUserAccessInfo != null)
            {
                _fileProvidersUserAccessInfo = null;
            }
        }

        #endregion IDisposable Members
    }
}