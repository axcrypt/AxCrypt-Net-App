using AxCrypt.Api.Model.User;

namespace AxCrypt.Core.StoreApi
{
    public interface IMembershipInfoStoreApiService
    {
        Task<MembershipInfoApiModel> GetUserByEmailAsync(long userId);

        Task<bool> UpdateIsNewsUnsubscribedAsync(MembershipInfoApiModel model);

        Task<bool> UpdateApiKeyAsync(MembershipInfoApiModel model);

        Task<bool> UpdateIsEmailInvalidAsync(MembershipInfoApiModel model);

        Task<bool> UpdateSubsAsync(MembershipInfoApiModel model);

        Task<bool> UpdateInvitedByAsync(MembershipInfoApiModel model);

        Task<bool> UpdateApplyPendingEmailChangeAsync(MembershipInfoApiModel model);

        Task<bool> UpdateUnsubscribedAsync(MembershipInfoApiModel model);
    }
}