using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ContractBotApi.Data;
using ContractBotApi.Models;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace ContractBotApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GPTController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public GPTController(HttpClient httpClient, IConfiguration configuration, ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GPTRequest request)
        {
            var apiKey = _configuration["OpenAIApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest("OpenAI API key not found in configuration.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "user", content = request.Prompt }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Save conversation history
                var conversationHistory = new ConversationHistory
                {
                    UserId = "anonymous", // You may want to implement user authentication
                    Message = request.Prompt,
                    Response = jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString(),
                    Timestamp = DateTime.UtcNow
                };
                _context.ConversationHistories.Add(conversationHistory);
                await _context.SaveChangesAsync();

                return Ok(jsonResponse);
            }
            else
            {
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
        }

        [HttpPost("upload-pdf")]
        public async Task<IActionResult> UploadPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only PDF files are allowed.");

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var text = ExtractTextFromPdf(memoryStream);

            return Ok(new { text });
        }

        private string ExtractTextFromPdf(Stream pdfStream)
        {
            using var reader = new PdfReader(pdfStream);
            var text = new StringBuilder();

            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
            }

            return text.ToString();
        }
    }

    public class GPTRequest
    {
        public string? Prompt { get; set; }
    }
}