using System;

namespace ContractBotApi.Models
{
    public class UploadedFile
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; }
        public string BlobStorageLocation { get; set; }
        public DateTime UploadTimestamp { get; set; }
    }
}