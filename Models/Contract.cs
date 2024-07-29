using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ContractBotApi.Data;
using Microsoft.Extensions.Logging;

namespace ContractBotApi.Models
{
    public abstract class Contract
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; }
        public string BlobStorageLocation { get; set; }
        public DateTime UploadTimestamp { get; set; }
        public string ContractText { get; set; }
        public string ContractType { get; set; }
        public string Product { get; set; }
        public string Price { get; set; }
        public string Volume { get; set; }
        public string? DeliveryTerms { get; set; }
        public string Appendix { get; set; }

        public abstract Task<string> GetSpecialFields();
        public abstract Task<string> GetFieldFormatting();
    }
}