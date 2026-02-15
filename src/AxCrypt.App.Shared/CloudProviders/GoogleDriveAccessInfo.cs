using System;
using Newtonsoft.Json;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Providers
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GoogleDriveAccessInfo
    {
        public GoogleDriveAccessInfo(string accessToken, long? expiresInSeconds, string refreshToken, string scope, string tokenType)
        {
            AccessToken = accessToken;
            ExpiresInSeconds = expiresInSeconds.HasValue ? expiresInSeconds.Value : 0;
            RefreshToken = refreshToken;
            Scope = scope;
            TokenType = tokenType;
            CreatedDateTimeUtc = New<Abstractions.INow>().Utc;
        }

        [JsonProperty("access_token")]
        public string AccessToken { get; private set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; private set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; private set; }

        [JsonProperty("expires_in")]
        public long ExpiresInSeconds { get; private set; }

        [JsonProperty("created_datetime_utc")]
        public DateTime CreatedDateTimeUtc { get; private set; } = New<Abstractions.INow>().Utc;
    }
}