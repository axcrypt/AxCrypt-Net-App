using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Core.StoreApi
{
    public interface ISPStoreApiCaller
    {
        void UpdateMoveUserAsync(string fromEmail, string toEmail);

        void DeleteUserEmail(string userEmail);
    }
}
