using System;

namespace ContractBotApi.Models
{
    public class OptionContract : Contract
    {
        public DateTime? ExpirationDate { get; set; }
        public string? StrikePrice { get; set; }
        public string? OptionType { get; set; } // "Call" or "Put"

        public override Task<string> GetSpecialFields()
        {
            return Task.FromResult("ExpirationDate,\nStrikePrice,\nOptionType");
        }

        public override Task<string> GetFieldFormatting()
        {
            return Task.FromResult("\"expiration_date\": \"{{{expiration date}}}\",\n\"strike_price\": \"{{{{strike price}}}}\",\n\"option_type\": \"{{{{option type}}}}\"");
        }
    }
}