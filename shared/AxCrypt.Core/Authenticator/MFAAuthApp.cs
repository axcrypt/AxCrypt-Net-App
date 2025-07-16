using AxCrypt.Core.Authenticator.Utility;
using System.Security.Cryptography;
using System.Text;

namespace AxCrypt.Core.Authenticator
{
    public enum HashType
    {
        SHA1, SHA256, SHA512
    }

    public class MFAAuthApp
    {
        //private static readonly DateTime _epoch = New<INow>().Utc;
        private static readonly DateTime _epoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly TimeSpan DefaultClockDriftTolerance;

        private readonly HashType HashType;

        private readonly int timeStep;

        public MFAAuthApp() : this(HashType.SHA1)
        { }

        public MFAAuthApp(HashType hashType) : this(hashType, 30)
        {
        }

        public MFAAuthApp(int timeStep) : this(HashType.SHA1, timeStep)
        { }

        public MFAAuthApp(HashType hashType, int timeStep)
        {
            HashType = hashType;
            DefaultClockDriftTolerance = TimeSpan.FromMinutes(5);
            this.timeStep = timeStep;
        }

        public string GeneratePINAtInterval(string accountSecretKey, long counter, int digits = 6, bool secretIsBase32 = false) =>
            GeneratePINAtInterval(ConvertSecretToBytes(accountSecretKey, secretIsBase32), counter, digits);

        public string GeneratePINAtInterval(byte[] accountSecretKey, long counter, int digits = 6) =>
            GenerateHashedCode(accountSecretKey, counter, digits);

        private string GenerateHashedCode(byte[] key, long iterationNumber, int digits = 6)
        {
            byte[] counter = BitConverter.GetBytes(iterationNumber);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(counter);

            HMAC hmac;
            if (HashType == HashType.SHA256)
                hmac = new HMACSHA256(key);
            else if (HashType == HashType.SHA512)
                hmac = new HMACSHA512(key);
            else
                hmac = new HMACSHA1(key);

            byte[] hash = hmac.ComputeHash(counter);
            int offset = hash[hash.Length - 1] & 0xf;

            int binary =
                ((hash[offset] & 0x7f) << 24)
                | (hash[offset + 1] << 16)
                | (hash[offset + 2] << 8)
                | hash[offset + 3];

            int password = binary % (int)Math.Pow(10, digits);
            return password.ToString(new string('0', digits));
        }

        private long GetCurrentCounter() => GetCurrentCounter(DateTime.UtcNow, _epoch);

        private long GetCurrentCounter(DateTime now, DateTime epoch) =>
            (long)(now - epoch).TotalSeconds / timeStep;

        public bool ValidateTwoFactorPIN(string accountSecretKey, string twoFactorCodeFromClient, bool secretIsBase32 = false) =>
            ValidateTwoFactorPIN(accountSecretKey, twoFactorCodeFromClient, DefaultClockDriftTolerance, secretIsBase32);

        public bool ValidateTwoFactorPIN(string accountSecretKey, string twoFactorCodeFromClient, TimeSpan timeTolerance, bool secretIsBase32 = false) =>
            ValidateTwoFactorPIN(ConvertSecretToBytes(accountSecretKey, secretIsBase32), twoFactorCodeFromClient, timeTolerance);

        public bool ValidateTwoFactorPIN(byte[] accountSecretKey, string twoFactorCodeFromClient) =>
            ValidateTwoFactorPIN(accountSecretKey, twoFactorCodeFromClient, DefaultClockDriftTolerance);

        public bool ValidateTwoFactorPIN(byte[] accountSecretKey, string twoFactorCodeFromClient, TimeSpan timeTolerance) =>
            GetCurrentPINs(accountSecretKey, timeTolerance).Any(c => c == twoFactorCodeFromClient);

        public bool ValidateTwoFactorPIN(string accountSecretKey, string twoFactorCodeFromClient, int iterationOffset, bool secretIsBase32 = false) =>
            ValidateTwoFactorPIN(ConvertSecretToBytes(accountSecretKey, secretIsBase32), twoFactorCodeFromClient, iterationOffset);

        public bool ValidateTwoFactorPIN(byte[] accountSecretKey, string twoFactorCodeFromClient, int iterationOffset) =>
            GetCurrentPINs(accountSecretKey, iterationOffset).Any(c => c == twoFactorCodeFromClient);

        public string GetCurrentPIN(string accountSecretKey, bool secretIsBase32 = false) =>
            GeneratePINAtInterval(accountSecretKey, GetCurrentCounter(), secretIsBase32: secretIsBase32);

        public string GetCurrentPIN(string accountSecretKey, DateTime now, bool secretIsBase32 = false) =>
            GeneratePINAtInterval(accountSecretKey, GetCurrentCounter(now, _epoch), secretIsBase32: secretIsBase32);

        public string GetCurrentPIN(byte[] accountSecretKey) =>
            GeneratePINAtInterval(accountSecretKey, GetCurrentCounter());

        public string GetCurrentPIN(byte[] accountSecretKey, DateTime now) =>
            GeneratePINAtInterval(accountSecretKey, GetCurrentCounter(now, _epoch));

        public string[] GetCurrentPINs(string accountSecretKey, bool secretIsBase32 = false) =>
            GetCurrentPINs(accountSecretKey, DefaultClockDriftTolerance, secretIsBase32);

        public string[] GetCurrentPINs(string accountSecretKey, TimeSpan timeTolerance, bool secretIsBase32 = false) =>
            GetCurrentPINs(ConvertSecretToBytes(accountSecretKey, secretIsBase32), timeTolerance);

        public string[] GetCurrentPINs(byte[] accountSecretKey) =>
            GetCurrentPINs(accountSecretKey, DefaultClockDriftTolerance);

        public string[] GetCurrentPINs(byte[] accountSecretKey, TimeSpan timeTolerance)
        {
            int iterationOffset = 0;

            if (timeTolerance.TotalSeconds >= timeStep)
                iterationOffset = Convert.ToInt32(timeTolerance.TotalSeconds / timeStep);

            return GetCurrentPINs(accountSecretKey, iterationOffset);
        }

        public string[] GetCurrentPINs(byte[] accountSecretKey, int iterationOffset)
        {
            IList<string> codes = new List<string>();
            long iterationCounter = GetCurrentCounter();

            long iterationStart = iterationCounter - iterationOffset;
            long iterationEnd = iterationCounter + iterationOffset;

            for (long counter = iterationStart; counter <= iterationEnd; counter++)
            {
                codes.Add(GeneratePINAtInterval(accountSecretKey, counter));
            }

            return codes.ToArray();
        }

        private byte[] ConvertSecretToBytes(string secret, bool secretIsBase32)
        {
            if (secretIsBase32)
            {
                return Base32Encoding.ToBytes(secret);
            }
            else
            {
                return Encoding.UTF8.GetBytes(secret);
            }
        }
    }
}