using System;

namespace ContractBotApi.Models
{
    public class OptionContract : Contract
    {
        public DateTime? ExpirationDate { get; set; }
        public string? StrikePrice { get; set; }
        public string? OptionType { get; set; } // "Call" or "Put"
    }
}