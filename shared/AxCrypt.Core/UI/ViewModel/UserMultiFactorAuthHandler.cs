using AxCrypt.Abstractions;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.MFA;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.UI.ViewModel
{
    public static class UserMultiFactorAuthHandler
    {
        public static async Task<MultiFactorAuthType> GetMultiFactorStatusAsync(string userEmail, string currentDeviceInfo, Func<string, Task<MultiFactorAuthApiModel>> getMultiFactorAuthStatusApiModel)
        {
            MultiFactorAuthApiModel authStatusApiModel = await getMultiFactorAuthStatusApiModel(userEmail);
            return GetValidMFAAuthType(authStatusApiModel, currentDeviceInfo);
        }

        public static MultiFactorAuthType GetValidMFAAuthType(MultiFactorAuthApiModel authStatusApiModel, string currentDeviceInfo)
        {
            if (authStatusApiModel == null || string.IsNullOrEmpty(authStatusApiModel.MfaEnabledTypes))
            {
                return MultiFactorAuthType.None;
            }

            if (!Enum.TryParse(authStatusApiModel.MfaEnabledTypes, out MultiFactorAuthType selectedMultiFactorAuthType))
            {
                return MultiFactorAuthType.None;
            }

            if (string.IsNullOrEmpty(authStatusApiModel.UserDevice))
            {
                return selectedMultiFactorAuthType;
            }

            if (!CheckUserDeviceExpiry(authStatusApiModel, currentDeviceInfo))
            {
                return MultiFactorAuthType.None;
            }

            return selectedMultiFactorAuthType;
        } 

        private static bool CheckUserDeviceExpiry(MultiFactorAuthApiModel authStatusApiModel, string currentDeviceInfo)
        {
            if (authStatusApiModel.RememberUntil == null || string.IsNullOrEmpty(authStatusApiModel.UserDevice))
            {
                return false;
            }

            if (New<INow>().Utc > authStatusApiModel.RememberUntil)
            {
                return true;
            }

            byte[] encodedDeviceInfo = Convert.FromBase64String(authStatusApiModel.UserDevice);
            string decodeUserDevice = System.Text.Encoding.UTF8.GetString(encodedDeviceInfo, 0, encodedDeviceInfo.Length);
            if (decodeUserDevice != currentDeviceInfo)
            {
                return true;
            }

            return false;
        }
    }
}