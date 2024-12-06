using AxCrypt.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.UI
{
    public abstract class VerifySignInPasswordBase : IVerifySignInPassword
    {
        public async Task<bool> Verify(string description)
        {
            if (!New<KnownIdentities>().IsLoggedOn)
            {
                return false;
            }

            bool isVerified = await VerifyDialog(description);
            return isVerified;
        }

        protected abstract Task<bool> VerifyDialog(string description);
    }
}