using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ContractBotApi.Models
{
    public class Contract
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; }
        public string BlobStorageLocation { get; set; }
        public DateTime UploadTimestamp { get; set; }
        public string ContractText { get; set; }
        public string ContractType { get; set; }
        public string? Product { get; set; }
        public string? Price { get; set; }
        public string? Volume { get; set; }
        public string? DeliveryTerms { get; set; }
        public string? Appendix { get; set; }

        public async Task ExtractContractDataAsync(HttpClient httpClient, string apiKey, string contractText, ILogger logger)
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
                                text = "You are a state of the art contract parsing engine. The user will submit a contract in plain text format, and you will classify the contract into one of four possible types, and extract certain important data points and respond with this information in JSON format.\n\nThe four possible contract types are: \nSpot Contract,\nForward Contract,\nOption Contract,\nSwap Contract\n\nBesides making this classification, you will extract these data points from the contract text:\nProduct (e.g. hydrogen, SAF, ammonia),\nPrice,\nVolume,\nDeliveryTerms,\nAppendix containing legal terms.\n\nThe JSON response should be in this format:\n\"\"\"\n{\n  \"contract_type\": \"Forward Contract\",\n  \"product\": \"Industrial grade Hydrogen\",\n  \"price\": \"$3 per kilogram\",\n  \"volume\": \"10,000 kilograms per month\",\n  \"delivery_terms\": \"Delivered at Place (DAP) as defined in Incoterms 2020\",\n  \"appendix\": \"Hydrogen Purity: 99.97%, Moisture Content: less than 5 ppm, Delivery Pressure: 500 psi\"\n}\n\"\"\""
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
                                text = contractText
                            }
                        }
                    }
                },
                temperature = 1,
                max_tokens = 256,
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
                
                // Remove any leading or trailing backticks, whitespace, and the "json" prefix
                extractedDataString = extractedDataString.Trim('`', ' ', '\n', '\r');
                if (extractedDataString.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                {
                    extractedDataString = extractedDataString.Substring(4).TrimStart();
                }

                logger.LogInformation("Extracted data string: {ExtractedDataString}", extractedDataString);

                try
                {
                    var extractedData = JsonSerializer.Deserialize<JsonElement>(extractedDataString);

                    ContractType = extractedData.TryGetProperty("contract_type", out var contractType) ? contractType.GetString() : null;
                    Product = extractedData.TryGetProperty("product", out var product) ? product.GetString() : null;
                    Price = extractedData.TryGetProperty("price", out var price) ? price.GetString() : null;
                    Volume = extractedData.TryGetProperty("volume", out var volume) ? volume.GetString() : null;
                    DeliveryTerms = extractedData.TryGetProperty("delivery_terms", out var deliveryTerms) ? deliveryTerms.GetString() : null;
                    Appendix = extractedData.TryGetProperty("appendix", out var appendix) ? appendix.GetString() : null;
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
    }
}