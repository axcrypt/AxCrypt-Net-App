namespace AxCrypt.Api.Extension
{
    public static class CipherTypeExtension
    {
        public static string GetCipherString(this byte[] cipher)
        {
            return Convert.ToBase64String(cipher);
        }

        public static byte[] GetCipherBytes(this string cipher)
        {
            return Convert.FromBase64String(cipher);
        }
    }
}