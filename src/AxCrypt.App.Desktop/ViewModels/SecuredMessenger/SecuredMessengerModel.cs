using AxCrypt.Api.SecuredMessenger;
using AxCrypt.Core.SecuredMessenger;
using AxCrypt.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AxCrypt.App.Desktop.ViewModels.SecuredMessenger
{
    public class SecuredMessengerModel
    {
        public IEnumerable<SecuredMessage> Messages { get; set; } = Enumerable.Empty<SecuredMessage>();

        public SecureMsgrFilterTab SecMessengerFilterTab { get; set; }

        public int PageNumber { get; set; } = 0;

        public string Keyword { get; set; }

        public SecureMsgrSearchFilters SecMsgSearchFilters { get; set; } = SecureMsgrSearchFilters.OneWeek;

        public IEnumerable<string> SecMesgFilterOptions
        {
            get
            {
                return Enum.GetNames(typeof(SecureMsgrSearchFilters));
            }
        }

        public string ErrorMessage { get; internal set; }
    }
}