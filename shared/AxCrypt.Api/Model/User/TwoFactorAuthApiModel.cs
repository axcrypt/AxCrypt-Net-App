using AxCrypt.Api.Model.Notification;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AxCrypt.Api.Model.User
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TwoFactorAuthApiModel : BaseApiModel
    {
        public TwoFactorAuthApiModel()
        {
        }

        public TwoFactorAuthApiModel(long id, string userEmail, string uniqueKey, string backupCodeSalt, string backupCodeHash, bool isValid)
        {
            Id = id;
            UserEmail = userEmail;
            UniqueKey = uniqueKey;
            BackupCodeSalt = backupCodeSalt;
            BackupCodeHash = backupCodeHash;
        }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("userEmail")]
        public string UserEmail { get; set; }

        [JsonProperty("uniqueKey")]
        public string UniqueKey { get; set; }

        [JsonProperty("backupCodeSalt")]
        public string BackupCodeSalt { get; set; }

        [JsonProperty("backupCodeHash")]
        public string BackupCodeHash { get; set; }
    }
}