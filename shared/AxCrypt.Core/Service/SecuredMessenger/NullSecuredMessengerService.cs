#region Coypright and License

/*
 * AxCrypt AB - Copyright 2023, All Rights Reserved
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

using AxCrypt.Api;
using AxCrypt.Api.Model.SecuredMessenger;
using AxCrypt.Api.SecuredMessenger;
using AxCrypt.Core.Crypto;

namespace AxCrypt.Core.Service.SecuredMessenger
{
    public class NullSecuredMessengerService : ISecuredMessengerService
    {
        private static readonly Task<bool> _completedTask = Task.FromResult(true);

        public NullSecuredMessengerService(LogOnIdentity identity)
        {
            Identity = identity;
        }

        public ISecuredMessengerService Refresh()
        {
            return this;
        }

        public LogOnIdentity Identity
        {
            get; private set;
        }

        public Task<IEnumerable<SecuredMessengerApiModel>> GetListAsync(RequestOptions requestOptions)
        {
            IEnumerable<SecuredMessengerApiModel> getList = new List<SecuredMessengerApiModel>();
            return Task.FromResult(getList as IEnumerable<SecuredMessengerApiModel>);
        }

        public Task<IEnumerable<SecuredMessengerApiModel>> GetSentListAsync(RequestOptions requestOptions)
        {
            IEnumerable<SecuredMessengerApiModel> getSentList = new List<SecuredMessengerApiModel>();
            return Task.FromResult(getSentList as IEnumerable<SecuredMessengerApiModel>);
        }

        public Task<IEnumerable<SecuredMessengerApiModel>> GetUnreadListAsync(RequestOptions requestOptions)
        {
            IEnumerable<SecuredMessengerApiModel> getUnreadList = new List<SecuredMessengerApiModel>();
            return Task.FromResult(getUnreadList as IEnumerable<SecuredMessengerApiModel>);
        }

        public Task<bool> CreateAsync(SecuredMessengerApiModel messengerApiModel)
        {
            return _completedTask;
        }

        public Task<SecuredMessengerRootApiModel> GetAsync(Guid id, string userEmail)
        {
            return Task.FromResult(new SecuredMessengerRootApiModel());
        }

        public Task<bool> UpdateAsync(IEnumerable<Guid> ids, string userEmail, bool isUnread)
        {
            return _completedTask;
        }

        public Task<bool> DeleteAsync(IEnumerable<Guid> ids, string user, SecureMsgrFilterTab securedMessengerFilter)
        {
            return _completedTask;
        }

        public Task<bool> SaveMessagelist(SecuredMessengerApiModel model)
        {
            return _completedTask;
        }

        public Task<bool> SavemessagesAsync(SecuredMessengerRootApiModel model)
        {
            return _completedTask;
        }

        public Task<IEnumerable<SecuredMessengerRootApiModel>> GetSecMsgWithSearchFiltersAsync(SecureMsgrFilterTab securedMessengerFilterTab, RequestOptions requestOptions)
        {
            throw new NotImplementedException();
        }
    }
}