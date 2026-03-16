using System;
using System.Collections.Generic;
using System.Linq;

namespace AxCrypt.Api.Model
{
    public enum SubscriptionLevel
    {
        Unknown,
        DefinedByServer,
        Undisclosed,
        LegacyFree,
        Free,
        Premium,
        Business,
        PasswordManager
    }
}