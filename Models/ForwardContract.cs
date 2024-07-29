using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ContractBotApi.Models
{
    public class ForwardContract : Contract
    {
        public DateTime? FutureDeliveryDate { get; set; }
        public string? SettlementTerms { get; set; }
        public string? ForwardPrice { get; set; }

        public override Task<string> GetSpecialFields()
        {
            return Task.FromResult("FutureDeliveryDate,\nSettlementTerms,\nForwardPrice");
        }

        public override Task<string> GetFieldFormatting() // Implementing the abstract method
        {
            return Task.FromResult("\"future_delivery_date\": \"{{{future delivery date}}}\",\n\"settlement_terms\": \"{{{{settlement terms}}}}\",\n\"forward_price\": \"{{{{forward price}}}}\"");
        }
    }
}