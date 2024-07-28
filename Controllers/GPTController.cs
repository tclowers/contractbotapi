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
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

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

        public GPTController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ApplicationDbContext context, BlobServiceClient blobServiceClient, ILogger<GPTController> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
            _context = context;
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ContractGPTRequest request)
        {
            var apiKey = _configuration["OpenAIApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest("OpenAI API key not found in configuration.");
            }

            try
            {
                var response = await request.Contract.ContractPrompt(_httpClient, apiKey, request.Prompt);

                // Save conversation history
                var conversationHistory = new ConversationHistory
                {
                    UserId = "anonymous", // You may want to implement user authentication
                    Message = request.Prompt,
                    Response = response,
                    Timestamp = DateTime.UtcNow
                };
                _context.ConversationHistories.Add(conversationHistory);
                await _context.SaveChangesAsync();

                return Ok(new { response });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, ex.Message);
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

                var blobName = file.FileName;
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

                var contract = new Contract
                {
                    OriginalFileName = file.FileName,
                    BlobStorageLocation = blobClient.Uri.ToString(),
                    UploadTimestamp = DateTime.UtcNow,
                    ContractText = text
                };

                // Extract contract data using OpenAI API
                _logger.LogInformation("Extracting contract data using OpenAI API");
                var apiKey = _configuration["OpenAIApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return BadRequest("OpenAI API key not found in configuration.");
                }
                try
                {
                    bool isContract = await contract.ExtractContractDataAsync(_httpClient, apiKey, text, _logger);
                    if (!isContract)
                    {
                        return Ok(new { 
                            isContract = false,
                            message = "The uploaded document is not a contract."
                        });
                    }

                    _logger.LogInformation("Contract data extracted successfully");
                    Contract contractToAdd;
                    switch (contract.ContractType.ToLower())
                    {
                        case "forward contract":
                            var forwardContract = new ForwardContract
                            {
                                OriginalFileName = contract.OriginalFileName,
                                BlobStorageLocation = contract.BlobStorageLocation,
                                UploadTimestamp = contract.UploadTimestamp,
                                ContractText = contract.ContractText,
                                ContractType = contract.ContractType,
                                Product = contract.Product,
                                Price = contract.Price,
                                Volume = contract.Volume,
                                DeliveryTerms = contract.DeliveryTerms,
                                Appendix = contract.Appendix,
                            };
                            // TODO: This should be implemented for other contract types
                            await forwardContract.ExtractForwardContractDataAsync(_httpClient, apiKey, _logger);
                            contractToAdd = forwardContract;
                            break;
                        case "spot contract":
                            contractToAdd = new SpotContract
                            {
                                OriginalFileName = contract.OriginalFileName,
                                BlobStorageLocation = contract.BlobStorageLocation,
                                UploadTimestamp = contract.UploadTimestamp,
                                ContractText = contract.ContractText,
                                ContractType = contract.ContractType,
                                Product = contract.Product,
                                Price = contract.Price,
                                Volume = contract.Volume,
                                DeliveryTerms = contract.DeliveryTerms,
                                Appendix = contract.Appendix,
                            };
                            break;
                        case "option contract":
                            contractToAdd = new OptionContract
                            {
                                OriginalFileName = contract.OriginalFileName,
                                BlobStorageLocation = contract.BlobStorageLocation,
                                UploadTimestamp = contract.UploadTimestamp,
                                ContractText = contract.ContractText,
                                ContractType = contract.ContractType,
                                Product = contract.Product,
                                Price = contract.Price,
                                Volume = contract.Volume,
                                DeliveryTerms = contract.DeliveryTerms,
                                Appendix = contract.Appendix,
                            };
                            break;
                        case "swap contract":
                            contractToAdd = new SwapContract
                            {
                                OriginalFileName = contract.OriginalFileName,
                                BlobStorageLocation = contract.BlobStorageLocation,
                                UploadTimestamp = contract.UploadTimestamp,
                                ContractText = contract.ContractText,
                                ContractType = contract.ContractType,
                                Product = contract.Product,
                                Price = contract.Price,
                                Volume = contract.Volume,
                                DeliveryTerms = contract.DeliveryTerms,
                                Appendix = contract.Appendix,
                            };
                            break;
                        default:
                            contractToAdd = contract;
                            break;
                    }

                    _context.Contracts.Add(contractToAdd);
                    await _context.SaveChangesAsync();

                    object response;

                    if (contractToAdd is ForwardContract forwardContractDetails)
                    {
                        response = new
                        {
                            isContract = true,
                            fileId = contractToAdd.Id,
                            originalFileName = contractToAdd.OriginalFileName,
                            blobStorageLocation = contractToAdd.BlobStorageLocation,
                            contractText = contractToAdd.ContractText,
                            contractType = contractToAdd.ContractType,
                            product = contractToAdd.Product,
                            price = contractToAdd.Price,
                            volume = contractToAdd.Volume,
                            deliveryTerms = contractToAdd.DeliveryTerms,
                            appendix = contractToAdd.Appendix,
                            futureDeliveryDate = forwardContractDetails.FutureDeliveryDate,
                            settlementTerms = forwardContractDetails.SettlementTerms,
                            forwardPrice = forwardContractDetails.ForwardPrice
                        };
                    }
                    else
                    {
                        response = new
                        {
                            isContract = true,
                            fileId = contractToAdd.Id,
                            originalFileName = contractToAdd.OriginalFileName,
                            blobStorageLocation = contractToAdd.BlobStorageLocation,
                            contractText = contractToAdd.ContractText,
                            contractType = contractToAdd.ContractType,
                            product = contractToAdd.Product,
                            price = contractToAdd.Price,
                            volume = contractToAdd.Volume,
                            deliveryTerms = contractToAdd.DeliveryTerms,
                            appendix = contractToAdd.Appendix
                        };
                    }

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting contract data: {Message}", ex.Message);
                    return StatusCode(500, $"Error extracting contract data: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {Message}", ex.Message);
                return StatusCode(500, $"Error processing file: {ex.Message}");
            }
        }

        [HttpPost("generate-pdf")]
        public async Task<IActionResult> GeneratePdf([FromBody] PdfGenerationRequest request)
        {
            if (string.IsNullOrEmpty(request.FileName) || string.IsNullOrEmpty(request.TextContent))
            {
                return BadRequest("FileName and TextContent are required.");
            }

            try
            {
                // Generate PDF
                byte[] pdfBytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    Document document = new Document();
                    PdfWriter writer = PdfWriter.GetInstance(document, ms);
                    document.Open();
                    document.Add(new Paragraph(request.TextContent));
                    document.Close();
                    pdfBytes = ms.ToArray();
                }

                // Upload to Azure Blob Storage
                var containerClient = _blobServiceClient.GetBlobContainerClient("pdfs");
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient(request.FileName);

                using (MemoryStream stream = new MemoryStream(pdfBytes))
                {
                    await blobClient.UploadAsync(stream, true);
                }

                return Ok(new { message = "PDF generated and uploaded successfully", blobUrl = blobClient.Uri.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating or uploading PDF: {Message}", ex.Message);
                return StatusCode(500, $"Error generating or uploading PDF: {ex.Message}");
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

    public class PdfGenerationRequest
    {
        public string FileName { get; set; }
        public string TextContent { get; set; }
    }

    public class ContractGPTRequest
    {
        public Contract Contract { get; set; }
        public string Prompt { get; set; }
    }
}