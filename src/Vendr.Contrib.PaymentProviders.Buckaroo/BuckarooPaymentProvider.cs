using BuckarooSdk;
using BuckarooSdk.Constants;
using BuckarooSdk.DataTypes;
using BuckarooSdk.DataTypes.RequestBases;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Linq;
using System.Web;
using Vendr.Core;
using Vendr.Core.Models;
using Vendr.Core.Web.Api;
using Vendr.Core.Web.PaymentProviders;

namespace Vendr.Contrib.PaymentProviders.Buckaroo
{
    [PaymentProvider("buckaroo", "Buckaroo", "Buckaroo payment provider for one time payments")]
    public class BuckarooPaymentProvider : PaymentProviderBase<BuckarooSettings>
    {
        public override bool CanFetchPaymentStatus => true;
        public override bool CanCapturePayments => false;
        public override bool CanCancelPayments => true;
        public override bool CanRefundPayments => false;

        // Don't finalize at continue as we will finalize async via webhook
        public override bool FinalizeAtContinueUrl => false;

        internal SdkClient BuckarooClient { get; }

        public BuckarooPaymentProvider(VendrContext vendr) 
            : base(vendr)
        {
            BuckarooClient = new SdkClient();
        }

        public override string GetContinueUrl(OrderReadOnly order, BuckarooSettings settings)
        {
            settings.MustNotBeNull(nameof(settings));
            settings.ContinueUrl.MustNotBeNull("settings.ContinueUrl");

            return settings.ContinueUrl;
        }

        public override string GetCancelUrl(OrderReadOnly order, BuckarooSettings settings)
        {
            settings.MustNotBeNull(nameof(settings));
            settings.CancelUrl.MustNotBeNull("settings.CancelUrl");

            return settings.CancelUrl;
        }

        public override string GetErrorUrl(OrderReadOnly order, BuckarooSettings settings)
        {
            settings.MustNotBeNull(nameof(settings));
            settings.ErrorUrl.MustNotBeNull("settings.ErrorUrl");

            return settings.ErrorUrl;
        }

        public override PaymentFormResult GenerateForm(OrderReadOnly order, string continueUrl, string cancelUrl, string callbackUrl, BuckarooSettings settings)
        {
            var currency = Vendr.Services.CurrencyService.GetCurrency(order.CurrencyId);
            var currencyCode = currency.Code.ToUpperInvariant();

            var data = new TransactionBase
            {
                AmountDebit = order.TotalPrice.Value.WithTax,
                Currency = currencyCode,
                Description = order.OrderNumber,
                Invoice = order.OrderNumber,
                Order = order.OrderNumber,
                ReturnUrl = continueUrl,
                ReturnUrlCancel = cancelUrl,
                ReturnUrlError = GetErrorUrl(order, settings), // ToDo: the error url is different than the provided continue and cancel urls (webhook urls),
                                                               // we probably want this from the method's arguments as well
                ReturnUrlReject = cancelUrl,
                StartRecurrent = false,
                ClientIp = new IpAddress { Address = HttpContext.Current.Request.UserHostAddress, Type = InternetProtocolVersion.IPv4 }, // ToDo: should we have a look at IPv6 as well?
                ClientUserAgent = HttpContext.Current.Request.UserAgent,
                ContinueOnIncomplete = ContinueOnIncomplete.RedirectToHTML,
                PushUrl = callbackUrl,
                PushUrlFailure = callbackUrl
            };

            var response = BuckarooClient
                .CreateRequest()
                .Authenticate(settings.WebsiteKey, settings.SecretKey, !settings.TestMode, CultureInfo.CurrentCulture)
                .TransactionRequest()
                .SetBasicFields(data)
                .NoServiceSelected() // ToDo: for now it's fine to select a service at Buckaroo,
                                     // but it might be nice to implement available services etc.
                .Pay()
                .Execute();

            return new PaymentFormResult
            {
                Form = new PaymentForm(response.RequiredAction.RedirectURL)
            };
        }

        private static BuckarooWebhookRequest GetWebhookRequest(HttpRequestBase request)
        {
            // Transform postdata to json and deserialize
            var parameters = request.Params
                .Cast<string>()
                .ToDictionary(
                    k => k,
                    v => request.Params[v]);

            var json = JsonConvert.SerializeObject(parameters);
            return JsonConvert.DeserializeObject<BuckarooWebhookRequest>(json);
        }

        public override CallbackResult ProcessCallback(OrderReadOnly order, HttpRequestBase request, BuckarooSettings settings)
        {
            try
            {
                var buckarooRequest = GetWebhookRequest(request);

                if (!buckarooRequest.IsSuccess)
                {
                    return CallbackResult.Ok();
                }

                var transactionInfo = new TransactionInfo
                {
                    TransactionId = buckarooRequest.PaymentId,
                    PaymentStatus = PaymentStatus.Captured,
                    AmountAuthorized = buckarooRequest.Amount
                };

                return CallbackResult.Ok(transactionInfo);
            }
            catch (Exception ex)
            {
                Vendr.Log.Error<BuckarooPaymentProvider>(ex, "Buckaroo - ProcessCallback");
            }

            return CallbackResult.BadRequest();
        }

        // This should work in theory, but it's not used by Vendr yet...
        public override ApiResult FetchPaymentStatus(OrderReadOnly order, BuckarooSettings settings)
        {
            try
            {
                var status = BuckarooClient
                    .CreateRequest()
                    .Authenticate(settings.WebsiteKey, settings.SecretKey, !settings.TestMode, CultureInfo.CurrentCulture)
                    .TransactionStatusRequest()
                    .Status(order.TransactionInfo.TransactionId)
                    .GetSingleStatus();

                var statusCode = status.Status.Code.Code;
                return new ApiResult
                {
                    TransactionInfo = new TransactionInfoUpdate
                    {
                        TransactionId = status.PaymentKey,
                        PaymentStatus = statusCode == Status.Success ? PaymentStatus.Captured : statusCode == Status.CanceledByMerchant || statusCode == Status.CanceledByUser ? PaymentStatus.Cancelled : PaymentStatus.Error
                    }
                };
            }
            catch (Exception ex)
            {
                Vendr.Log.Error<BuckarooPaymentProvider>(ex, "Buckaroo - FetchPaymentStatus");
            }

            return ApiResult.Empty;
        }

        public override ApiResult CancelPayment(OrderReadOnly order, BuckarooSettings settings)
        {
            try
            {
                if (order.TransactionInfo.PaymentStatus == PaymentStatus.Authorized)
                {
                    var response = BuckarooClient
                        .CreateRequest()
                        .Authenticate(settings.WebsiteKey, settings.SecretKey, !settings.TestMode, CultureInfo.CurrentCulture)
                        .CancelTransactionRequest()
                        .CancelMultiple(new CancelTransactionBase(order.TransactionInfo.TransactionId))
                        .Execute();

                    return new ApiResult
                    {
                        TransactionInfo = new TransactionInfoUpdate
                        {
                            TransactionId = response.PaymentKey,
                            PaymentStatus = PaymentStatus.Cancelled
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Vendr.Log.Error<BuckarooPaymentProvider>(ex, "Buckaroo - CancelPayment");
            }

            return ApiResult.Empty;
        }
    }
}
