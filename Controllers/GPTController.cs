using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ContractBotApi.Data;
using ContractBotApi.Models;
using Microsoft.Extensions.Logging;

namespace ContractBotApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GPTController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GPTController> _logger;

        public GPTController(IConfiguration configuration, ApplicationDbContext context, IHttpClientFactory httpClientFactory, ILogger<GPTController> logger)
        {
            _configuration = configuration;
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task HandleWebSocket(HttpContext httpContext)
        {
            try
            {
                _logger.LogInformation("WebSocket request received");
                
                if (httpContext == null)
                {
                    _logger.LogError("HttpContext is null");
                    return;
                }

                if (httpContext.WebSockets == null)
                {
                    _logger.LogError("HttpContext.WebSockets is null");
                    return;
                }

                if (httpContext.WebSockets.IsWebSocketRequest)
                {
                    _logger.LogInformation("Accepting WebSocket request");
                    using var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
                    _logger.LogInformation("WebSocket accepted, starting Echo");
                    await Echo(webSocket);
                }
                else
                {
                    _logger.LogWarning("Request is not a WebSocket request");
                    httpContext.Response.StatusCode = 400;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the HandleWebSocket method");
                throw;
            }
        }

        private async Task Echo(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var response = await ProcessGPTRequest(message);

                var serverMsg = Encoding.UTF8.GetBytes(response);
                await webSocket.SendAsync(new ArraySegment<byte>(serverMsg, 0, serverMsg.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        private async Task<string> ProcessGPTRequest(string prompt)
        {
            var apiKey = _configuration["OpenAIApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return "OpenAI API key not found in configuration.";
            }

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "user", content = prompt }
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
                    Message = prompt,
                    Response = jsonResponse.ToString(),
                    Timestamp = DateTime.UtcNow
                };
                _context.ConversationHistories.Add(conversationHistory);
                await _context.SaveChangesAsync();

                return jsonResponse.ToString();
            }
            else
            {
                return $"Error: {response.StatusCode}";
            }
        }
    }
}