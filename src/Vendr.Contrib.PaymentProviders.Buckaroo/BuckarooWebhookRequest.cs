using BuckarooSdk.Constants;
using BuckarooSdk.DataTypes.Response;
using Newtonsoft.Json;
using System;

namespace Vendr.Contrib.PaymentProviders.Buckaroo
{
    public class BuckarooWebhookRequest
    {
        [JsonProperty("brq_transactions")]
        public string PaymentId { get; set; }

        [JsonProperty("brq_amount")]
        public decimal Amount { get; set; }

        [JsonProperty("brq_currency")]
        public string Currency { get; set; }

        [JsonProperty("brq_customer_name")]
        public string CustomerName { get; set; }

        [JsonProperty("brq_description")]
        public string Description { get; set; }

        [JsonProperty("brq_invoicenumber")]
        public string InvoiceNumber { get; set; }

        [JsonProperty("brq_mutationtype")]
        public MutationType MutationType { get; set; }

        [JsonProperty("brq_ordernumber")]
        public string OrderNumber { get; set; }

        [JsonProperty("brq_payer_hash")]
        public string PayerHash { get; set; }

        [JsonProperty("brq_payment")]
        public string Payment { get; set; }

        [JsonProperty("brq_statuscode")]
        public int Statuscode { get; set; }

        [JsonProperty("brq_statuscode_detail")]
        public string StatuscodeDetail { get; set; }

        [JsonProperty("brq_statusmessage")]
        public string StatusMessage { get; set; }

        [JsonProperty("brq_test")]
        public bool IsTest { get; set; }

        [JsonProperty("brq_timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("brq_transaction_method")]
        public string TransactionMethod { get; set; }

        [JsonProperty("brq_transaction_type")]
        public string TransactionType { get; set; }

        [JsonProperty("brq_websitekey")]
        public string WebsiteKey { get; set; }

        [JsonProperty("brq_signature")]
        public string Signature { get; set; }

        public bool IsSuccess => Statuscode == Status.Success;
        public bool IsCancelled => Statuscode == Status.CanceledByMerchant || Statuscode == Status.CanceledByUser;
    }
}
