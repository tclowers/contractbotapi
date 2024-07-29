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
using Microsoft.EntityFrameworkCore;

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
        private readonly ContractService _contractService;
        private readonly PdfService _pdfService;

        public GPTController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ApplicationDbContext context, BlobServiceClient blobServiceClient, ILogger<GPTController> logger, ContractService contractService, PdfService pdfService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
            _context = context;
            _blobServiceClient = blobServiceClient;
            _logger = logger;
            _contractService = contractService;
            _pdfService = pdfService;
        }

        [HttpPost("contract/{id}/prompt")]
        public async Task<IActionResult> Post(int id, [FromBody] GPTRequest request)
        {
            var apiKey = _configuration["OpenAIApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest("OpenAI API key not found in configuration.");
            }

            try
            {
                var contract = await _context.Contracts.FindAsync(id);
                if (contract == null)
                {
                    return NotFound($"Contract with ID {id} not found.");
                }

                var response = await _contractService.ContractPrompt(contract, request.Prompt);

                // Save conversation history
                var conversationHistory = new ConversationHistory
                {
                    UserId = "anonymous", // You may want to implement user authentication
                    Message = request.Prompt,
                    Response = JsonSerializer.Serialize(response),
                    Timestamp = DateTime.UtcNow
                };
                _context.ConversationHistories.Add(conversationHistory);
                await _context.SaveChangesAsync();

                // Fetch the updated contract details
                var updatedContract = await _context.Contracts.FindAsync(id);

                object contractDetails;
                if (updatedContract is ForwardContract forwardContract)
                {
                    contractDetails = new
                    {
                        id = forwardContract.Id,
                        contractType = forwardContract.ContractType,
                        product = forwardContract.Product,
                        price = forwardContract.Price,
                        volume = forwardContract.Volume,
                        deliveryTerms = forwardContract.DeliveryTerms,
                        appendix = forwardContract.Appendix,
                        futureDeliveryDate = forwardContract.FutureDeliveryDate,
                        settlementTerms = forwardContract.SettlementTerms,
                        forwardPrice = forwardContract.ForwardPrice
                    };
                }
                else
                {
                    contractDetails = new
                    {
                        id = updatedContract.Id,
                        contractType = updatedContract.ContractType,
                        product = updatedContract.Product,
                        price = updatedContract.Price,
                        volume = updatedContract.Volume,
                        deliveryTerms = updatedContract.DeliveryTerms,
                        appendix = updatedContract.Appendix
                    };
                }

                return Ok(new { response, updatedContract = contractDetails });
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
                // Check if a contract with the same filename already exists
                var existingContract = await _context.Contracts
                    .FirstOrDefaultAsync(c => c.OriginalFileName == file.FileName);

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
                    text = _pdfService.ExtractTextFromPdf(stream);
                }
                _logger.LogInformation("Text extracted, length: {Length}", text.Length);

                var classificationResult = await _contractService.ClassifyContractDataAsync(text);
                if (!classificationResult.IsContract)
                {
                    return Ok(new { isContract = false, message = "The uploaded document is not a contract." });
                }
                _logger.LogInformation("Classification result: {ClassificationResult}", classificationResult);

                string contractType = classificationResult.ContractType;
                Contract contract = ContractFactory.CreateContract(contractType);
                contract.ContractType = contractType;
                await _contractService.ExtractContractDataAsync(contract, text);

                // Ensure all required fields are populated
                contract.OriginalFileName = file.FileName;
                contract.BlobStorageLocation = blobClient.Uri.ToString();
                contract.UploadTimestamp = DateTime.UtcNow;
                contract.ContractText = text;
                contract.DeliveryTerms = contract.DeliveryTerms ?? "Not specified";
                contract.Product = contract.Product ?? "Not specified";
                contract.Price = contract.Price ?? "Not specified";
                contract.Volume = contract.Volume ?? "Not specified";
                contract.Appendix = contract.Appendix ?? "Not specified";

                if (existingContract != null)
                {
                    // Update existing contract
                    contract.Id = existingContract.Id; // Preserve the existing Id
                    _context.Entry(existingContract).CurrentValues.SetValues(contract);
                    _context.Entry(existingContract).State = EntityState.Modified;
                }
                else
                {
                    // Create new contract
                    _context.Contracts.Add(contract);
                }

                try
                {
                    await _context.SaveChangesAsync();

                    var response = new
                    {
                        isContract = true,
                        id = contract.Id,
                        originalFileName = contract.OriginalFileName,
                        blobStorageLocation = contract.BlobStorageLocation,
                        contractText = contract.ContractText,
                        contractType = contract.ContractType,
                        product = contract.Product,
                        price = contract.Price,
                        volume = contract.Volume,
                        deliveryTerms = contract.DeliveryTerms,
                        appendix = contract.Appendix
                    };

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving contract data: {Message}", ex.Message);
                    return StatusCode(500, $"Error saving contract data: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {Message}", ex.Message);
                return StatusCode(500, $"Error processing file: {ex.Message}");
            }
        }

        [HttpPatch("edit-contract/{id}")]
        public async Task<IActionResult> EditContract(int id, [FromBody] PdfGenerationRequest request)
        {
            if (string.IsNullOrEmpty(request.TextContent))
            {
                return BadRequest("TextContent is required.");
            }

            try
            {
                // Fetch the contract from the database
                var contract = await _context.Contracts.FindAsync(id);
                if (contract == null)
                {
                    return NotFound($"Contract with ID {id} not found.");
                }

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

                var blobClient = containerClient.GetBlobClient(contract.OriginalFileName);

                using (MemoryStream stream = new MemoryStream(pdfBytes))
                {
                    await blobClient.UploadAsync(stream, true);
                }

                // Extract contract data
                var apiKey = _configuration["OpenAIApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return BadRequest("OpenAI API key not found in configuration.");
                }

                // Update the contract in the database
                contract.ContractText = request.TextContent;
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "PDF generated, uploaded, and contract data updated successfully", 
                    blobUrl = blobClient.Uri.ToString(),
                    updatedContract = new
                    {
                        id = contract.Id,
                        contractType = contract.ContractType,
                        product = contract.Product,
                        price = contract.Price,
                        volume = contract.Volume,
                        deliveryTerms = contract.DeliveryTerms,
                        appendix = contract.Appendix
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating or uploading PDF: {Message}", ex.Message);
                return StatusCode(500, $"Error generating or uploading PDF: {ex.Message}");
            }
        }

        [HttpGet("contracts")]
        public async Task<IActionResult> GetContracts()
        {
            try
            {
                var contracts = await _context.Contracts
                    .Select(c => new { c.Id, c.OriginalFileName })
                    .ToListAsync();

                return Ok(contracts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contracts: {Message}", ex.Message);
                return StatusCode(500, $"Error retrieving contracts: {ex.Message}");
            }
        }

        [HttpGet("contract/{id}")]
        public async Task<IActionResult> GetContract(int id)
        {
            try
            {
                var contract = await _context.Contracts
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (contract == null)
                {
                    return NotFound($"Contract with ID {id} not found.");
                }

                var forwardContract = await _context.Set<ForwardContract>()
                    .FirstOrDefaultAsync(fc => fc.Id == id);

                object response;

                if (forwardContract != null)
                {
                    response = new
                    {
                        isContract = true,
                        id = contract.Id,
                        originalFileName = contract.OriginalFileName,
                        blobStorageLocation = contract.BlobStorageLocation,
                        contractText = contract.ContractText,
                        contractType = contract.ContractType,
                        product = contract.Product,
                        price = contract.Price,
                        volume = contract.Volume,
                        deliveryTerms = contract.DeliveryTerms,
                        appendix = contract.Appendix,
                        futureDeliveryDate = forwardContract.FutureDeliveryDate,
                        settlementTerms = forwardContract.SettlementTerms,
                        forwardPrice = forwardContract.ForwardPrice
                    };
                }
                else
                {
                    response = new
                    {
                        isContract = true,
                        id = contract.Id,
                        originalFileName = contract.OriginalFileName,
                        blobStorageLocation = contract.BlobStorageLocation,
                        contractText = contract.ContractText,
                        contractType = contract.ContractType,
                        product = contract.Product,
                        price = contract.Price,
                        volume = contract.Volume,
                        deliveryTerms = contract.DeliveryTerms,
                        appendix = contract.Appendix
                    };
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contract: {Message}", ex.Message);
                return StatusCode(500, $"Error retrieving contract: {ex.Message}");
            }
        }
    }

    public class GPTRequest
    {
        public string Prompt { get; set; }
    }

    public class PdfGenerationRequest
    {
        public string TextContent { get; set; }
    }
}