using System;
using System.Threading.Tasks;
using AxCrypt.Core.IO;

namespace AxCrypt.App.Shared.CloudCore
{
    /// <summary>
    /// Represents abstraction for file which stored in external cloud storage application. 
    /// The some preparation can be required before working with such files - e.g. saving file on disk.
    /// In iOS application the native UIDocument.Open() method should be called before accessing file. When work with file is ended, the UIDocument.Close() method should be called.
    /// The iOS implementation of IDataStore interface invokes these methods in every member (e.g IDataStore.OpenRead() or IDataStore.IsAvailable). 
    /// But during decryption these members are used frequently, so file can be opened and closed multiple times. 
    /// To avoid this problem the IExternalDataStore.OpenAsync() method should be invoked before the long manipulations with file. 
    /// See usage sample in IExternalDataStore declaration.
    /// Detailed info about iOS file lifecycle: 
    /// https://developer.apple.com/library/content/documentation/DataManagement/Conceptual/DocumentBasedAppPGiOS/ManageDocumentLifeCycle/ManageDocumentLifeCycle.html
    /// </summary>
	public interface IExternalDataStore : IDataStore
	{
        //public async void SampleOfUsage()
        //{
        //    IExternalDataStore dataStore = smth;
        //    // Invokes the native Open() method once
        //    using (await dataStore.OpenAsync())
        //    {
        //        // Performs frequent operations with members IDataStore here:
        //        bool isAvailable = dataStore.IsAvailable;
        //        using (dataStore.OpenRead())
        //        {

        //            dataStore.SetFileTimes(new DateTime(), new DateTime(), new DateTime());
        //        }
        //
        //    // Invokes the native Close() method via disposing file opening token.
        //    }
        //}


        /// <summary>
        /// Opens external file for accessing it properties.
        /// </summary>
        /// <returns>Asynchronous operation which returns token for closing file, when we don't need it anymore.</returns>
        Task<IDisposable> OpenAsync();
	}
}
