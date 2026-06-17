using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Api.Model.User
{
    public enum UserActivityEvents
    {
        None = 0,
        AccountCreationStarted,
        AccountCreationCompleted,
        PlanSetup,
        CheckoutPage,
        BusinessInformationPage,
        PurchaseSuccess,
        PurchaseFailed
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class UserActivityApiModel : BaseApiModel
    {
        public UserActivityApiModel()
        {

        }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("useremail")]
        public string UserEmail { get; set; }

        [JsonProperty("activityevent")]
        public UserActivityEvents ActivityEvent { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
