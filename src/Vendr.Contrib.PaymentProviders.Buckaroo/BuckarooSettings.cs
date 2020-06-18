using Vendr.Core.Web.PaymentProviders;

namespace Vendr.Contrib.PaymentProviders.Buckaroo
{
    public class BuckarooSettings
    {
        [PaymentProviderSetting(Name = "Continue URL", Description = "The URL to continue to after this provider has done processing. eg: /continue/", SortOrder = 100)]
        public string ContinueUrl { get; set; }

        [PaymentProviderSetting(Name = "Cancel URL", Description = "The URL to return to if the payment attempt is canceled. eg: /cancel/", SortOrder = 200)]
        public string CancelUrl { get; set; }

        [PaymentProviderSetting(Name = "Error URL", Description = "The URL to return to if the payment attempt errors. eg: /error/", SortOrder = 300)]
        public string ErrorUrl { get; set; }

        [PaymentProviderSetting(Name = "Website key", Description = "The website key, which can be found here: https://plaza.buckaroo.nl/Configuration/WebSite/Index/", SortOrder = 400)]
        public string WebsiteKey { get; set; }

        [PaymentProviderSetting(Name = "Secret key", Description = "The secret key, which can be found here: https://plaza.buckaroo.nl/Configuration/Merchant/SecretKey", SortOrder = 500)]
        public string SecretKey { get; set; }

        [PaymentProviderSetting(Name = "Test mode", Description = "Set whether to process payments in test mode", SortOrder = 10000)]
        public bool TestMode { get; set; }
    }
}
