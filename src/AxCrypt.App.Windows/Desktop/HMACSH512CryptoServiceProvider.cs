using AxCrypt.Mono.Cryptography;

namespace AxCrypt.App.Windows.Desktop;

public class HMACSHA512CryptoServiceProvider : HMACBase
{
    public HMACSHA512CryptoServiceProvider()
    {
        SetHash1(new System.Security.Cryptography.SHA512CryptoServiceProvider());
        SetHash2(new System.Security.Cryptography.SHA512CryptoServiceProvider());
        HashSizeValue = 512;
        BlockSizeValue = 128;
    }
}
