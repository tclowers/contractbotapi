using System;

namespace ContractBotApi.Models
{
    public class SwapContract : Contract
    {
        public string? UnderlyingAsset { get; set; }
        public string? NotionalAmount { get; set; }
        public string? PaymentFrequency { get; set; }
    }
}