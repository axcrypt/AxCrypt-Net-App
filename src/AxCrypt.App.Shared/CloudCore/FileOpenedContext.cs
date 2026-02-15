using System;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;

namespace AxCrypt.App.Shared.CloudCore
{
    /// <summary>
    /// Represents results of file decryption process.
    /// </summary>
    public class FileOpenedContext : FileOperationContext
    {
        public FileOpenedContext(FileOperationContext context, ActiveFile activeFile)
            : base(context.FullName, context.InternalMessage, context.ErrorStatus)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            AddedFile = activeFile;
        }

        /// <summary>
        /// Gets the file, which was decrypted and ready for opening.
        /// </summary>
        public ActiveFile AddedFile { get; private set; }
    }
}