using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ContractBotApi.Models
{
    public class SpotContract : Contract
    {
        public DateTime? SettlementDate { get; set; }
        public string? PaymentMethod { get; set; }
        public bool ImmediateDelivery { get; set; } = true;
    }
}