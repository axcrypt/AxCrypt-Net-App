using Microsoft.AspNetCore.DataProtection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Core.Secrets
{
    public static class KeyProtector
    {
        private static IDataProtector _protector;

        public static void Initialize(IDataProtectionProvider dataProtectionProvider)
        {
            _protector = dataProtectionProvider.CreateProtector("PrivateKeyProtector");
        }

        public static string Protect(byte[] privateKeyPemBytes)
        {
            if (_protector == null)
                throw new InvalidOperationException("KeyProtector is not initialized.");

            return _protector.Protect(Convert.ToBase64String(privateKeyPemBytes));
        }

        public static byte[] Unprotect(string protectedKey)
        {
            if (_protector == null)
                throw new InvalidOperationException("KeyProtector is not initialized.");

            try
            {
                string unprotectedString = _protector.Unprotect(protectedKey);
                return Convert.FromBase64String(unprotectedString);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}