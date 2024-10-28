using AxCrypt.Reports.Abstractions;
using System;
using System.IO;
using System.Reflection;

namespace AxCrypt.Reports.WinForms.Implementation
{
    internal class FileTextPersistence : ITextPersistence
    {
        public TextReader LoadFrom(PersistentName name)
        {
            return new StreamReader(new FileStream(RepositoryPath(name), FileMode.OpenOrCreate, FileAccess.Read));
        }

        public TextWriter SaveTo(PersistentName name)
        {
            return new StreamWriter(new FileStream(RepositoryPath(name), FileMode.Create, FileAccess.Write));
        }

        public void ClearAll(PersistentName name)
        {
            if (File.Exists(RepositoryPath(name)))
            {
                File.Delete(RepositoryPath(name));
            }
        }

        private static string RepositoryPath(PersistentName name)
        {
            return Path.Combine(MyFolder, $"{name.Name}.txt");
        }

        private static string MyFolder
        {
            get
            {
                return Path.GetDirectoryName(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath);
            }
        }
    }
}