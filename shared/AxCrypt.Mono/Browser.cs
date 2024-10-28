using AxCrypt.Abstractions;
using System;
using System.Diagnostics;
using System.IO;

namespace AxCrypt.Mono
{
    public class Browser : IBrowser
    {
        public void OpenUri(Uri url)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = url.ToString(),
                UseShellExecute = true,// This will use the default application associated with the file type
            };

            Process.Start(psi);
        }
    }
}