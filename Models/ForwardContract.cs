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

        public async Task<bool> ExtractForwardContractDataAsync(HttpClient httpClient, string apiKey, ILogger logger)
        {
            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = @"You are a state of the art contract parsing engine specialized in Forward Contracts. Extract the following information from the contract text: Future Delivery Date, Settlement Terms, and Forward Price. Respond with this information in JSON format.

Example response format:
{
    ""FutureDeliveryDate"": ""January 1, 2024"",
    ""SettlementTerms"": ""Net 30 days from the date of delivery and invoice date"",
    ""ForwardPrice"": ""3 per kilogram""
}"
                            }
                        }
                    },
                    new
                    {
                        role = "user",
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = this.ContractText
                            }
                        }
                    }
                },
                temperature = 1,
                max_tokens = 4096,
                top_p = 1,
                frequency_penalty = 0,
                presence_penalty = 0
            };

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var extractedDataString = jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                
                logger.LogInformation("Extracted data string: {ExtractedDataString}", extractedDataString);

                // Remove markdown formatting
                extractedDataString = RemoveMarkdownFormatting(extractedDataString);

                try
                {
                    var extractedData = JsonSerializer.Deserialize<JsonElement>(extractedDataString);

                    if (extractedData.TryGetProperty("FutureDeliveryDate", out var futureDeliveryDate) && futureDeliveryDate.ValueKind != JsonValueKind.Null)
                    {
                        if (DateTime.TryParse(futureDeliveryDate.GetString(), out var parsedDate))
                        {
                            // Convert to UTC before assigning
                            FutureDeliveryDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                        }
                    }

                    if (extractedData.TryGetProperty("SettlementTerms", out var settlementTerms) && settlementTerms.ValueKind != JsonValueKind.Null)
                    {
                        SettlementTerms = settlementTerms.GetString();
                    }

                    if (extractedData.TryGetProperty("ForwardPrice", out var forwardPrice) && forwardPrice.ValueKind != JsonValueKind.Null)
                    {
                        ForwardPrice = forwardPrice.GetString();
                    }

                    return true;
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, "Error parsing OpenAI API response: {Message}. Raw response: {ExtractedDataString}", ex.Message, extractedDataString);
                    throw new Exception($"Error parsing OpenAI API response: {ex.Message}. Raw response: {extractedDataString}");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Error calling OpenAI API: {StatusCode}. Response content: {ErrorContent}", response.StatusCode, errorContent);
                throw new Exception($"Error calling OpenAI API: {response.StatusCode}. Response content: {errorContent}");
            }
        }

        private string RemoveMarkdownFormatting(string input)
        {
            // Remove triple backticks and "json" label
            input = System.Text.RegularExpressions.Regex.Replace(input, @"```json\s*", "");
            input = System.Text.RegularExpressions.Regex.Replace(input, @"```\s*$", "");
            
            // Trim any leading or trailing whitespace
            return input.Trim();
        }
    }
}