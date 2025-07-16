using System;
using System.Collections.Generic;
using System.Linq;

namespace AxCrypt.Api.Model
{
    [Flags]
    public enum MultiFactorAuthType
    {
        None = 0x0,

        Authenticator = 0x1,

        Email = 0x2,

        SMS = 0x4,
    }
}