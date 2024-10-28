using System.IO;

namespace AxCrypt.Reports.Abstractions
{
    public interface ITextPersistence
    {
        TextWriter SaveTo(PersistentName name);

        TextReader LoadFrom(PersistentName name);

        void ClearAll(PersistentName name);
    }
}