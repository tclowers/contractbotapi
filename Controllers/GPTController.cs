using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ContractBotApi.Data;
using ContractBotApi.Models;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Linq;
using System.Text.RegularExpressions;

namespace ContractBotApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GPTController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<GPTController> _logger;

        public GPTController(HttpClient httpClient, IConfiguration configuration, ApplicationDbContext context, BlobServiceClient blobServiceClient, ILogger<GPTController> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _context = context;
            _blobServiceClient = blobServiceClient;
            _logger = logger;
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
            _logger.LogInformation("UploadPdf method called");

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file uploaded");
                return BadRequest("No file uploaded.");
            }

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Non-PDF file uploaded: {FileName}", file.FileName);
                return BadRequest("Only PDF files are allowed.");
            }

            try
            {
                // Upload to Azure Blob Storage
                _logger.LogInformation("Getting blob container client");
                var containerClient = _blobServiceClient.GetBlobContainerClient("pdfs");
                await containerClient.CreateIfNotExistsAsync();

                var blobName = $"{Guid.NewGuid()}.pdf";
                var blobClient = containerClient.GetBlobClient(blobName);

                _logger.LogInformation("Uploading to blob storage");
                await using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, true);
                }
                _logger.LogInformation("Upload to blob storage complete");

                // Extract text from PDF
                _logger.LogInformation("Extracting text from PDF");
                string text;
                using (var stream = file.OpenReadStream())
                {
                    text = ExtractTextFromPdf(stream);
                }
                _logger.LogInformation("Text extracted, length: {Length}", text.Length);

                var uploadedFile = new UploadedFile
                {
                    OriginalFileName = file.FileName,
                    BlobStorageLocation = blobClient.Uri.ToString(),
                    UploadTimestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Saving file information to database");
                _context.UploadedFiles.Add(uploadedFile);
                await _context.SaveChangesAsync();
                _logger.LogInformation("File information saved to database");

                return Ok(new { 
                    text, 
                    fileId = uploadedFile.Id,
                    originalFileName = uploadedFile.OriginalFileName,
                    blobStorageLocation = uploadedFile.BlobStorageLocation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {Message}", ex.Message);
                return StatusCode(500, $"Error processing file: {ex.Message}");
            }
        }

        private string ExtractTextFromPdf(Stream pdfStream)
        {
            var sb = new StringBuilder();

            using (var document = UglyToad.PdfPig.PdfDocument.Open(pdfStream))
            {
                for (var i = 1; i <= document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i);
                    var words = page.GetWords().ToList();
                    
                    if (!words.Any())
                    {
                        sb.AppendLine(); // Empty page
                        continue;
                    }

                    float lastBottom = (float)words[0].BoundingBox.Bottom;
                    float lineHeight = words.Max(w => (float)w.BoundingBox.Height);
                    float pageLeft = words.Min(w => (float)w.BoundingBox.Left);
                    StringBuilder lineSb = new StringBuilder();

                    bool isFirstLine = true;

                    foreach (var word in words)
                    {
                        float wordBottom = (float)word.BoundingBox.Bottom;
                        float wordLeft = (float)word.BoundingBox.Left;

                        if (lastBottom - wordBottom > lineHeight / 2)
                        {
                            // New line detected
                            if (isFirstLine)
                            {
                                sb.AppendLine(InsertSpacesIntoLongWords(lineSb.ToString().TrimEnd()));
                                isFirstLine = false;
                            }
                            else
                            {
                                sb.AppendLine(lineSb.ToString().TrimEnd());
                            }
                            lineSb.Clear();
                            
                            // Add empty lines if the gap is large enough
                            int emptyLines = (int)((lastBottom - wordBottom) / lineHeight) - 1;
                            for (int k = 0; k < emptyLines; k++)
                            {
                                sb.AppendLine();
                            }

                            // Add indentation
                            if (wordLeft - pageLeft > 10) // Adjust this value as needed
                            {
                                lineSb.Append("    "); // Add 4 spaces for indentation
                            }
                        }
                        else if (lineSb.Length > 0)
                        {
                            // Add space between words on the same line
                            lineSb.Append(' ');
                        }

                        lineSb.Append(word.Text);
                        lastBottom = wordBottom;
                    }

                    // Add the last line
                    sb.AppendLine(lineSb.ToString().TrimEnd());
                }
            }

            return sb.ToString().TrimEnd();
        }

        private string InsertSpacesIntoLongWords(string input)
        {
            var words = input.Split(' ');
            var result = new StringBuilder();

            foreach (var word in words)
            {
                if (word.Length > 10) // Adjust this threshold as needed
                {
                    result.Append(string.Join(" ", SplitCamelCase(word)));
                }
                else
                {
                    result.Append(word);
                }
                result.Append(' ');
            }

            return result.ToString().TrimEnd();
        }

        private IEnumerable<string> SplitCamelCase(string input)
        {
            return System.Text.RegularExpressions.Regex.Split(input, @"(?<!^)(?=[A-Z])");
        }
    }

    public class GPTRequest
    {
        public string? Prompt { get; set; }
    }
}