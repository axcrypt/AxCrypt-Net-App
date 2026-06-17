using Newtonsoft.Json;
using System;

namespace AxCrypt.Api.Model.User
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UserApiModel : UserAccountActivityApiModel
    {
        public static UserApiModel Empty = new UserApiModel(0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, DateTime.MinValue, DateTime.MinValue);

        public UserApiModel()
        {
        }

        public UserApiModel(long id, string userEmail, string role, string passwordSalt, string passwordHash, string activationCode, DateTime createdUtc, DateTime updatedUtc)
        {
            Id = id;
            UserEmail = userEmail;
            Role = role;
            PasswordSalt = passwordSalt;
            PasswordHash = passwordHash;
            ActivationCode = activationCode;
            CreatedUtc = createdUtc;
            UpdatedUtc = updatedUtc;
        }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("passwordSalt")]
        public string PasswordSalt { get; set; }

        [JsonProperty("passwordHash")]
        public string PasswordHash { get; set; }
       
        [JsonProperty("activationCode")]
        public string ActivationCode { get; set; }

        [JsonProperty("isapproved")]
        public bool IsApproved { get; set; }

        [JsonProperty("pendingemailchangefrom")]
        public string PendingEmailChangeFrom { get; set; } = string.Empty;

        [JsonProperty("multifactortype")]
        public string MultiFactorType { get; set; }

        [JsonProperty("signupFrom")]
        public string SignupFrom { get; set; }
    }
}