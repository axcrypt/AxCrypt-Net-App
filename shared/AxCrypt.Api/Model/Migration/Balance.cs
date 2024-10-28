using System;
using Newtonsoft.Json;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]

    public class Balance : BaseApiModel
    {
        public Balance(long id,DateTime balancedatetimeutc,string balancevatculture,string balancecurrency,double balanceamount, double balancevat, double balancefees, string paymenttransactionid,DateTime createdUtc,DateTime updatedUtc,DateTime? deletedUtc) 
        {
            Id=id;
            BalanceDateTimeUtc=balancedatetimeutc;
            BalanceVatCulture=balancevatculture;
            BalanceCurrency=balancecurrency;
            BalanceAmount=balanceamount;
            BalanceVat=balancevat;
            BalanceFees=balancefees;
            PaymentTransactionId=paymenttransactionid;
            CreatedUtc = createdUtc;
            UpdatedUtc= updatedUtc;
            DeletedUtc= deletedUtc;
        }

        [JsonProperty("bal_dt_utc")]
        public DateTime BalanceDateTimeUtc { get; set; }

        [JsonProperty("bal_vat_cult")]
        public string? BalanceVatCulture { get; set; }

        [JsonProperty("bal_cur")]
        public string BalanceCurrency { get; set; }

        [JsonProperty("bal_amt")]
        public double BalanceAmount { get; set; }

        [JsonProperty("bal_vat")]
        public double BalanceVat { get; set; }

        [JsonProperty("bal_fee")]
        public double BalanceFees { get; set; }

        [JsonProperty("pymt_trans_id")]
        public string PaymentTransactionId { get; set; }
    }
}
