using System;
using AxCrypt.Abstractions;
using System.Security.Cryptography;

namespace AxCrypt.Core.Crypto;

public static class CustomAesEncryption
{
    public static byte[] EncryptData(byte[] data, byte[] key, byte[] iv)
    {
        return EncryptWithIvPrepended(data, key, iv);
    }

    private static byte[] EncryptWithIvPrepended(byte[] data, byte[] key, byte[] iv)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        //aes.GenerateIV(); // generate a random IV
        //iv = aes.IV;

        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using MemoryStream ms = new MemoryStream();
        using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
        }

        byte[] encrypted = ms.ToArray();
        byte[] encryptedDataWithIV = new byte[iv.Length + encrypted.Length];

        Buffer.BlockCopy(iv, 0, encryptedDataWithIV, 0, iv.Length);
        Buffer.BlockCopy(encrypted, 0, encryptedDataWithIV, iv.Length, encrypted.Length);

        return encryptedDataWithIV;
    }

    public static byte[] DecryptData(byte[] encryptedData, byte[] key, byte[] iv)
    {
        if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            throw new ArgumentException("Invalid AES key length.");
        if (iv.Length != 16)
            throw new ArgumentException("Invalid AES IV length.");

        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (MemoryStream ms = new MemoryStream(encryptedData))
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (MemoryStream decryptedStream = new MemoryStream())
            {
                cs.CopyTo(decryptedStream);
                return decryptedStream.ToArray();
            }
        }
    }
    private static byte[] DecryptWithIvPrepended(byte[] encryptedWithIv, byte[] key)
    {
        byte[] iv = new byte[16];
        byte[] encrypted = new byte[encryptedWithIv.Length - 16];

        Buffer.BlockCopy(encryptedWithIv, 0, iv, 0, 16);
        Buffer.BlockCopy(encryptedWithIv, 16, encrypted, 0, encrypted.Length);

        return DecryptData(encrypted, key, iv);
    }

    public static bool TryUnprotect(byte[] protectedValue, byte[] key, out byte[] bytes)
    {
        bytes = null;
        try
        {
            bytes = DecryptWithIvPrepended(protectedValue, key);
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