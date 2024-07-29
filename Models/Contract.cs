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

        public async Task<object> ContractPrompt(HttpClient httpClient, string apiKey, string prompt, BlobServiceClient blobServiceClient, ApplicationDbContext context, ILogger logger)
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var systemPrompt = $@"You are a state of the art contract parsing, interpreting, and editing engine. The user is going to submit a prompt related to the contract text below.

If the user asks a question about the content of the contract, answer that user's question using the contract text as a reference.

Some examples of questions about the contract would be:
""Explain Clause 13b to me in concise terms""
""Summarize the content of the attached file""

When a user asks this type of question, respond in this JSON format:
{{
""prompt_type"": ""query"",
""prompt_response"": ""{{response text goes here}}""
}}

If the user submits a prompt that relates to making a change, or an edit to the contract, even if this request is in the form of a question, update the text of the contract accordingly and return it as the ""updated_text"" field, note that this type of prompt is a ""contract edit"", and send a short ""prompt_response"" explaining the change you have made.

When a user requests a contract edit, respond in this JSON format:
{{
""prompt_type"": ""contract_edit"",
""prompt_response"": ""{{response text goes here}}"",
""updated_text"": ""{{complete text of the updated contract goes here}}""
}}

Some examples of prompts that might result in an edit would be:
""Rewrite this contract, replacing 'biodiesel' with 'SAF' throughout the document""
""Can you change the delivery date to December 3, 2025?""

Here is the Contract Text. All prompts submitted by the user will be in reference to this contract text:
""""""
{this.ContractText}
""""""";

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = new[] { new { type = "text", text = systemPrompt } } },
                    new { role = "user", content = new[] { new { type = "text", text = prompt } } }
                },
                temperature = 1,
                max_tokens = 4096,
                top_p = 1,
                frequency_penalty = 0,
                presence_penalty = 0
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var gptResponse = jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                // Extract JSON content from the response
                var jsonStartIndex = gptResponse.IndexOf('{');
                var jsonEndIndex = gptResponse.LastIndexOf('}');
                if (jsonStartIndex >= 0 && jsonEndIndex >= 0 && jsonEndIndex > jsonStartIndex)
                {
                    gptResponse = gptResponse.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);
                }

                try
                {
                    var parsedResponse = JsonSerializer.Deserialize<JsonElement>(gptResponse);
                    var promptType = parsedResponse.GetProperty("prompt_type").GetString();
                    var promptResponse = parsedResponse.GetProperty("prompt_response").GetString();
                    var updatedText = promptType == "contract_edit" ? parsedResponse.GetProperty("updated_text").GetString() : null;

                    if (promptType == "contract_edit")
                    {
                        // Update Azure Blob Storage
                        var containerClient = blobServiceClient.GetBlobContainerClient("pdfs");
                        var blobClient = containerClient.GetBlobClient(this.OriginalFileName);
                        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(updatedText)))
                        {
                            await blobClient.UploadAsync(stream, true);
                        }

                        // Update database
                        this.ContractText = updatedText;
                        context.Update(this);
                        await context.SaveChangesAsync();

                        // Re-extract contract data
                        await this.ExtractContractDataAsync(httpClient, apiKey, updatedText, logger);
                    }

                    return new { 
                        prompt_type = promptType,
                        prompt_response = promptResponse,
                        updated_text = updatedText
                    };
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, "Error parsing GPT response: {Response}", gptResponse);
                    throw new Exception($"Error parsing GPT response: {ex.Message}. Raw response: {gptResponse}");
                }
            }
            else
            {
                throw new HttpRequestException($"Error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
            }
        }

        public async Task<bool> ExtractContractDataAsync(HttpClient httpClient, string apiKey, string contractText, ILogger logger)
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
                                text = "You are a state of the art contract parsing engine. The user will submit a document in plain text format that may be a contract, classify it as a contract or non-contract, and if it is a contract you will classify the contract into one of four possible types, and extract certain important data points and respond with this information in JSON format.\n\nThe four possible contract types are: \nSpot Contract,\nForward Contract,\nOption Contract,\nSwap Contract\n\nBesides making this classification, you will extract these data points from the contract text:\nProduct (e.g. hydrogen, SAF, ammonia),\nPrice,\nVolume,\nDeliveryTerms,\nAppendix containing legal terms.\n\nThe JSON response should be in this format:\n\"\"\"\n{\n  \"is_contract\": \"true\",\n  \"contract_type\": \"Forward Contract\",\n  \"product\": \"Industrial grade Hydrogen\",\n  \"price\": \"$3 per kilogram\",\n  \"volume\": \"10,000 kilograms per month\",\n  \"delivery_terms\": \"Delivered at Place (DAP) as defined in Incoterms 2020\",\n  \"appendix\": \"Hydrogen Purity: 99.97%, Moisture Content: less than 5 ppm, Delivery Pressure: 500 psi\"\n}\n\"\"\""
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
                
                // Remove any leading or trailing backticks, whitespace, triple quotes, and the "json" prefix
                extractedDataString = extractedDataString.Trim('`', ' ', '\n', '\r', '"');
                if (extractedDataString.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                {
                    extractedDataString = extractedDataString.Substring(4).TrimStart();
                }

                logger.LogInformation("Extracted data string: {ExtractedDataString}", extractedDataString);

                try
                {
                    var extractedData = JsonSerializer.Deserialize<JsonElement>(extractedDataString);

                    if (extractedData.TryGetProperty("is_contract", out var isContractElement))
                    {
                        bool isContract = isContractElement.GetString().Equals("true", StringComparison.OrdinalIgnoreCase);
                        if (!isContract)
                        {
                            return false;
                        }
                    }

                    ContractType = extractedData.TryGetProperty("contract_type", out var contractType) ? contractType.GetString() : null;
                    Product = extractedData.TryGetProperty("product", out var product) ? product.GetString() : null;
                    Price = extractedData.TryGetProperty("price", out var price) ? price.GetString() : null;
                    Volume = extractedData.TryGetProperty("volume", out var volume) ? volume.GetString() : null;
                    DeliveryTerms = extractedData.TryGetProperty("delivery_terms", out var deliveryTerms) ? deliveryTerms.GetString() : null;
                    Appendix = extractedData.TryGetProperty("appendix", out var appendix) ? appendix.GetString() : null;

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
    }
}