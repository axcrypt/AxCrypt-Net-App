using AxCrypt.Api.Model.Migration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Core.StoreApi
{
    public interface IXecretServiceStoreApi
    {
        Task<bool> CreateSubsEventAsync(XecretServiceApiModel subsEvent);

        Task<bool> UpdateSubsEventAsync(XecretServiceApiModel subsEvent);

        Task<bool> UpdateMoveUserSubsAsync(string fromEmail, string toEmail);

        Task<bool> CopyUserSubsAsync(string fromEmail, string toEmail);

        Task<IEnumerable<XecretServiceApiModel>> GetSubsEventAsync(string email);

        Task<bool> DeleteSubsEventAsync(string email);
    }
}
