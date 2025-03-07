using AxCrypt.Api;
using AxCrypt.Api.Model.SecuredMessenger;
using AxCrypt.Api.SecuredMessenger;
using AxCrypt.Core.Crypto;

#region Coypright and License

/*
 * AxCrypt - Copyright 2016, Svante Seleborg, All Rights Reserved
 *
 * This file is part of AxCrypt.
 *
 * AxCrypt is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * AxCrypt is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with AxCrypt.  If not, see <http://www.gnu.org/licenses/>.
 *
 * The source is maintained at http://bitbucket.org/AxCrypt-net please visit for
 * updates, contributions and contact with the author. You may also visit
 * http://www.axcrypt.net for more information about the author.
*/

#endregion Coypright and License

namespace AxCrypt.Core.Service.SecuredMessenger
{
    public interface ISecuredMessengerService
    {
        ISecuredMessengerService Refresh();

        LogOnIdentity Identity { get; }

        Task<IEnumerable<SecuredMessengerApiModel>> GetListAsync(RequestOptions requestOptions);

        Task<IEnumerable<SecuredMessengerApiModel>> GetSentListAsync(RequestOptions requestOptions);

        Task<IEnumerable<SecuredMessengerApiModel>> GetUnreadListAsync(RequestOptions requestOptions);

        Task<bool> CreateAsync(SecuredMessengerApiModel model);

        Task<bool> SaveMessagelist(SecuredMessengerApiModel model);

        Task<bool> SavemessagesAsync(SecuredMessengerRootApiModel model);

        Task<bool> UpdateAsync(IEnumerable<Guid> ids, string userEmail, bool isUnread = false);

        Task<SecuredMessengerRootApiModel> GetAsync(Guid id, string userEmail);

        Task<bool> DeleteAsync(IEnumerable<Guid> ids, string user, SecureMsgrFilterTab securedMessengerFilter);

        Task<IEnumerable<SecuredMessengerRootApiModel>> GetSecMsgWithSearchFiltersAsync(SecureMsgrFilterTab securedMessengerFilterTab, RequestOptions requestOptions);
    }
}