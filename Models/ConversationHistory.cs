using System;

namespace ContractBotApi.Models
{
    public class ConversationHistory
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
        public string Response { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
