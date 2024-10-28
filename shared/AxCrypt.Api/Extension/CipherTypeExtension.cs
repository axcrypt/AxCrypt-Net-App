namespace AxCrypt.Api.Extension
{
    public static class CipherTypeExtension
    {
        public static string GetCipherBytes(this byte[] cipher)
        {
            return Convert.ToBase64String(cipher);
        }

        public static byte[] GetCipherString(this string cipher)
        {
            return Convert.FromBase64String(cipher);
        }
    }
}