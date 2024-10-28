using System;
using Newtonsoft.Json;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]

    public class Charge : BaseApiModel
    {
        public Charge(long id,DateTime chargedatetimeutc,string chargevatculture,double chargevatrate,string chargecurrency,double chargeamount,double chargevat,double chargefees,string paymenttransactionid,DateTime createdUtc,DateTime updatedUtc,DateTime? deletedUtc) 
        {
            Id=id;
            ChargeDateTimeUtc=chargedatetimeutc;
            ChargeVatCulture=chargevatculture;
            ChargeVatRate=chargeamount;
            ChargeCurrency =chargecurrency;
            ChargeAmount=chargeamount;
            ChargeVat = chargevat;
            ChargeFees=chargefees;
            PaymentTransactionId=paymenttransactionid;
            CreatedUtc=createdUtc;
            UpdatedUtc=updatedUtc;
            DeletedUtc=deletedUtc;
        }


        [JsonProperty("charge_datetime_utc")]
        public DateTime ChargeDateTimeUtc { get; set; }

        [JsonProperty("charge_Vat_Culture")]
        public string ChargeVatCulture { get; set; }

        [JsonProperty("charge_vat_rate")]
        public double ChargeVatRate { get; set; }

        [JsonProperty("charge_cur")]
        public string ChargeCurrency { get; set; }

        [JsonProperty("charge_amt")]
        public double ChargeAmount { get; set; }

        [JsonProperty("charge_vat")]
        public double ChargeVat { get; set; }

        [JsonProperty("charge_fee")]
        public double ChargeFees { get; set; }

        [JsonProperty("pymt_trans_id")]
        public string PaymentTransactionId { get; set; }
    }
}
