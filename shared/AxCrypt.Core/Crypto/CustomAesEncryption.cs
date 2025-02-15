using System;
using AxCrypt.Abstractions;
using System.Security.Cryptography;

namespace AxCrypt.Core.Crypto;

public static class CustomAesEncryption
{
    public static byte[] EncryptData(byte[] data, byte[] key, byte[] iv)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                }
                return ms.ToArray();
            }
        }
    }

    private static byte[] DecryptData(byte[] encryptedData, byte[] key, byte[] iv)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;

            using (MemoryStream ms = new MemoryStream(encryptedData))
            {
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (MemoryStream decryptedStream = new MemoryStream())
                    {
                        cs.CopyTo(decryptedStream);
                        return decryptedStream.ToArray();
                    }
                }
            }
        }
    }

    public static bool TryUnprotect(byte[] protectedValue, byte[] key, byte[] iv, out byte[] bytes)
    {
        bytes = null;
        try
        {
            bytes = DecryptData(protectedValue, key, iv);
        }
        catch (AxCryptException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        return bytes != null;
    }
}