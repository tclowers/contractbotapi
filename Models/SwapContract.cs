using System;

namespace ContractBotApi.Models
{
    public class SwapContract : Contract
    {
        public string? UnderlyingAsset { get; set; }
        public string? NotionalAmount { get; set; }
        public string? PaymentFrequency { get; set; }

        public override Task<string> GetSpecialFields()
        {
            return Task.FromResult("UnderlyingAsset,\nNotionalAmount,\nPaymentFrequency");
        }

        public override Task<string> GetFieldFormatting()
        {
            return Task.FromResult("\"underlying_asset\": \"{{{underlying asset}}}\",\n\"notional_amount\": \"{{{{notional amount}}}}\",\n\"payment_frequency\": \"{{{{payment frequency}}}}\"");
        }
    }
}