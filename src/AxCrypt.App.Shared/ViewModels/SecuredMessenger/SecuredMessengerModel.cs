using AxCrypt.Abstractions;
using AxCrypt.Api.SecuredMessenger;
using AxCrypt.Core.SecuredMessenger;
using AxCrypt.Core.UI;
using System;
using System.Collections.Generic;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels.SecuredMessenger
{
    public class SecuredMessengerModel
    {
        private const int _defaultSearchFilerDays = 7;
        public SecuredMessengerModel(SecureMsgrFilterTab secMessengerFilterTab)
        {
            SecMessengerFilterTab = secMessengerFilterTab;
            StartDate = New<INow>().Utc.AddDays(-_defaultSearchFilerDays).ToLocalTime();
            EndDate = New<INow>().Utc.ToLocalTime();
        }

        public IList<SecuredMessage> Messages { get; set; } = new List<SecuredMessage>();

        public IList<SecuredMessage> ChildMessages { get; set; } = new List<SecuredMessage>();

        public SecureMsgrFilterTab SecMessengerFilterTab { get; set; }

        public int PageNumber { get; set; } = 0;

        public string Keyword { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string UserName { get; set; }

        public string ReceiverName { get; set; }

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