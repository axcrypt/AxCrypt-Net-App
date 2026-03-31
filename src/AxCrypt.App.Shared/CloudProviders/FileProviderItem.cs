using AxCrypt.Core.IO;

namespace AxCrypt.App.Shared.Providers;

public class FileProviderItem
{
    public FileProviderItem(string name, FileProvider value, string image, bool configured = false)
    {
        Name = name;
        Value = value;
        Image = image;
        Configured = configured;
    }
    public string? Name { get; set; }

    public FileProvider Value { get; set; }

    public string? Image { get; set; }

    public bool Configured { get; set; } = false;
}
