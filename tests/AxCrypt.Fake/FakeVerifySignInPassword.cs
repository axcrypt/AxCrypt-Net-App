using AxCrypt.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AxCrypt.Fake
{
    public class FakeVerifySignInPassword : IVerifySignInPassword
    {
        public async Task<bool> Verify(string description)
        {
            await Task.Delay(1000);
            bool result = false;
            return result;
        }
    }
}