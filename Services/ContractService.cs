using System.Net.Http;
using Microsoft.Extensions.Logging;
using ContractBotApi.Data;
using ContractBotApi.Models;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using iTextSharp.text;
using iTextSharp.text.pdf;

public class ContractService
{
    private readonly ApplicationDbContext _context;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<ContractService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ContractService(HttpClient httpClient, string apiKey, ApplicationDbContext context, BlobServiceClient blobServiceClient, ILogger<ContractService> logger)
    {
        _context = context;
        _blobServiceClient = blobServiceClient;
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public class ContractClassificationResult
    {
        public bool IsContract { get; set; }
        public string ContractType { get; set; }
    }

    public async Task<ContractClassificationResult> ClassifyContractDataAsync(string contractText)
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
                            text = "You are a state of the art contract parsing engine. The user will submit a document in plain text format that may be a contract, classify it as a contract or non-contract, and if it is a contract you will classify the contract into one of four possible types\n\nThe four possible contract types are: \nSpot Contract,\nForward Contract,\nOption Contract,\nSwap Contract\n\nYou must respond in JSON format.\n\nThe JSON response should be in this format:\n\"\"\"\n{\n  \"is_contract\": \"true\",\n  \"contract_type\": \"Forward Contract\"\n}\n\"\"\""
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
            temperature = 0.2,
            max_tokens = 1024,
            top_p = 1,
            frequency_penalty = 0,
            presence_penalty = 0
        };

        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

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

            _logger.LogInformation("Extracted data string: {ExtractedDataString}", extractedDataString);

            try
            {
                var extractedData = JsonSerializer.Deserialize<JsonElement>(extractedDataString);

                bool isContract = false;
                string contractType = null;

                if (extractedData.TryGetProperty("is_contract", out var isContractElement))
                {
                    isContract = isContractElement.GetString().Equals("true", StringComparison.OrdinalIgnoreCase);
                    if (isContract)
                    {
                        contractType = extractedData.TryGetProperty("contract_type", out var contractTypeElement) ? contractTypeElement.GetString() : null;
                    }
                }

                return new ContractClassificationResult
                {
                    IsContract = isContract,
                    ContractType = contractType
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing OpenAI API response: {Message}. Raw response: {ExtractedDataString}", ex.Message, extractedDataString);
                throw;
            }
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error calling OpenAI API: {StatusCode}. Response content: {ErrorContent}", response.StatusCode, errorContent);
            throw new Exception($"Error calling OpenAI API: {response.StatusCode}. Response content: {errorContent}");
        }
    }

    public async Task<bool> ExtractContractDataAsync(Contract contract, string contractText)
    {
        var specialFields = await contract.GetSpecialFields();
        var fieldFormatting = await contract.GetFieldFormatting();

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
                            text = $"You are a state of the art contract parsing engine. The user will submit a contract in plain text format. You will extract certain important data points and respond with this information in JSON format.\n\nYou will extract these data points from the contract text:\nProduct (e.g. hydrogen, SAF, ammonia),\nPrice,\nVolume,\nDelivery Terms,\nAppendix containing legal terms\n{specialFields}\n\nThe JSON response should be in this format:\n\"\"\"\n{{\"product\": \"{{{{contract product}}}}\",\n  \"price\": \"{{{{product price}}}}\",\n  \"volume\": \"{{{{volume of product}}}}\",\n  \"delivery_terms\": \"{{{{delivery terms}}}}\",\n  \"appendix\": \"{{{{appendix of useful legal terms}}}}\", {fieldFormatting}\n}}\n\"\"\""
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
            temperature = 0.2,
            max_tokens = 8192,
            top_p = 1,
            frequency_penalty = 0,
            presence_penalty = 0
        };

        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",   _apiKey);
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response content: {ResponseContent}", responseContent);
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var extractedDataString = jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            
            // Remove any leading or trailing backticks, whitespace, triple quotes, and the "json" prefix
            extractedDataString = extractedDataString.Trim('`', ' ', '\n', '\r', '"');
            if (extractedDataString.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                extractedDataString = extractedDataString.Substring(4).TrimStart();
            }

            _logger.LogInformation("Extracted data string: {ExtractedDataString}", extractedDataString);

            try
            {
                var extractedData = JsonSerializer.Deserialize<JsonElement>(extractedDataString);
                
                // Validate that required fields exist
                var requiredFields = new[] { "product", "price", "volume" };
                foreach (var field in requiredFields)
                {
                    if (!extractedData.TryGetProperty(field, out _))
                    {
                        _logger.LogWarning("Missing required field '{Field}' in extraction response", field);
                    }
                }

                foreach (var property in extractedData.EnumerateObject())
                {
                    var propertyInfo = contract.GetType().GetProperty(property.Name, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (propertyInfo != null && propertyInfo.CanWrite)
                    {
                        if (propertyInfo.PropertyType == typeof(DateTime?) && DateTime.TryParse(property.Value.GetString(), out var parsedDate))
                        {
                            propertyInfo.SetValue(contract, DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc));
                        }
                        else if (propertyInfo.PropertyType == typeof(bool) && bool.TryParse(property.Value.GetString(), out var parsedBool))
                        {
                            propertyInfo.SetValue(contract, parsedBool);
                        }
                        else
                        {
                            propertyInfo.SetValue(contract, property.Value.GetString());
                        }
                    }
                }

                return true;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing OpenAI API response: {Message}. Raw response: {ExtractedDataString}", ex.Message, extractedDataString);
                throw new Exception($"Error parsing OpenAI API response: {ex.Message}. Raw response: {extractedDataString}");
            }
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error calling OpenAI API: {StatusCode}. Response content: {ErrorContent}", response.StatusCode, errorContent);
            throw new Exception($"Error calling OpenAI API: {response.StatusCode}. Response content: {errorContent}");
        }
    }

    public async Task<object> ContractPrompt(Contract contract, string prompt)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        // Improved system prompt with clearer instructions
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

IMPORTANT: For edit requests like ""Change the product to Gold"" or ""Update the price to $500"", you MUST classify these as ""contract_edit"" type prompts and make the appropriate changes to the contract text.

When a user requests a contract edit, respond in this JSON format:
{{
""prompt_type"": ""contract_edit"",
""prompt_response"": ""{{response text goes here}}"",
""updated_text"": ""{{complete text of the updated contract goes here}}""
}}

Some examples of prompts that might result in an edit would be:
""Rewrite this contract, replacing 'biodiesel' with 'SAF' throughout the document""
""Can you change the delivery date to December 3, 2025?""
""Change the product to Gold""
""Update the price to $500 per ounce""

Here is the Contract Text. All prompts submitted by the user will be in reference to this contract text:
""""""
{contract.ContractText}
""""""";

        var requestBody = new
        {
            model = "gpt-4o",
            messages = new[]
            {
                new { role = "system", content = new[] { new { type = "text", text = systemPrompt } } },
                new { role = "user", content = new[] { new { type = "text", text = prompt } } }
            },
            temperature = 0.2,
            max_tokens = 16000,
            top_p = 1,
            frequency_penalty = 0,
            presence_penalty = 0
        };

        // Declare gptResponse at the method level so it's accessible in both try and catch blocks
        string gptResponse = string.Empty;

        try
        {
            // Get response from OpenAI
            gptResponse = await GetOpenAIResponseAsync(requestBody);
            
            // Sanitize and parse the JSON
            var sanitizedJson = SanitizeJsonResponse(gptResponse);
            _logger.LogInformation("Sanitized JSON: {SanitizedJson}", sanitizedJson);
            
            // Parse the JSON
            var parsedResponse = JsonSerializer.Deserialize<JsonElement>(sanitizedJson);
            
            // Check if required properties exist
            if (!parsedResponse.TryGetProperty("prompt_type", out var promptTypeElement))
            {
                throw new JsonException("Missing 'prompt_type' property in response");
            }
            
            var promptType = promptTypeElement.GetString();
            
            if (!parsedResponse.TryGetProperty("prompt_response", out var promptResponseElement))
            {
                throw new JsonException("Missing 'prompt_response' property in response");
            }
            
            var promptResponse = promptResponseElement.GetString();
            
            // Only try to get updated_text if it's a contract_edit
            string updatedText = null;
            if (promptType == "contract_edit")
            {
                if (!parsedResponse.TryGetProperty("updated_text", out var updatedTextElement))
                {
                    throw new JsonException("Missing 'updated_text' property in contract_edit response");
                }
                updatedText = updatedTextElement.GetString();
                
                if (string.IsNullOrWhiteSpace(updatedText))
                {
                    _logger.LogWarning("Empty updated_text received for contract_edit prompt: {Prompt}", prompt);
                    throw new JsonException("Empty 'updated_text' received for contract_edit");
                }

                // Create a new PDF with the updated text
                byte[] pdfBytes;
                using (var ms = new MemoryStream())
                {
                    using (var document = new Document())
                    {
                        PdfWriter.GetInstance(document, ms);
                        document.Open();
                        document.Add(new Paragraph(updatedText));
                        document.Close();
                    }
                    pdfBytes = ms.ToArray();
                }

                // Update Azure Blob Storage
                var containerClient = _blobServiceClient.GetBlobContainerClient("pdfs");
                var blobClient = containerClient.GetBlobClient(contract.OriginalFileName);
                using (var stream = new MemoryStream(pdfBytes))
                {
                    await blobClient.UploadAsync(stream, true);
                }

                // Update database
                contract.ContractText = updatedText;
                _context.Update(contract);
                await _context.SaveChangesAsync();

                // Re-extract contract data
                await ExtractContractDataAsync(contract, updatedText);
            }

            return new { 
                prompt_type = promptType,
                prompt_response = promptResponse,
                updated_text = updatedText
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GPT response: {Response}", gptResponse);
            
            // Enhanced error handling for JSON parsing issues
            if (gptResponse.Contains("updated_text") && gptResponse.Contains("prompt_type"))
            {
                _logger.LogWarning("Attempting manual JSON extraction as fallback");
                
                try {
                    // Manual extraction of key fields
                    var promptTypeMatch = Regex.Match(gptResponse, @"""prompt_type""\s*:\s*""([^""]+)""");
                    var promptResponseMatch = Regex.Match(gptResponse, @"""prompt_response""\s*:\s*""([^""]+)""");
                    
                    if (promptTypeMatch.Success && promptResponseMatch.Success)
                    {
                        var promptType = promptTypeMatch.Groups[1].Value;
                        var promptResponse = promptResponseMatch.Groups[1].Value;
                        
                        if (promptType == "contract_edit")
                        {
                            // For contract edits, try to extract the updated text
                            var updatedTextStart = gptResponse.IndexOf("\"updated_text\"");
                            if (updatedTextStart > 0)
                            {
                                var textStartIndex = gptResponse.IndexOf(":", updatedTextStart) + 1;
                                var textEndIndex = gptResponse.LastIndexOf("}");
                                
                                if (textStartIndex > 0 && textEndIndex > textStartIndex)
                                {
                                    var updatedText = gptResponse.Substring(textStartIndex, textEndIndex - textStartIndex).Trim();
                                    
                                    // Remove any quotes at the beginning and end
                                    updatedText = updatedText.TrimStart('"', ' ').TrimEnd('"', ' ', ',');
                                    
                                    // If we have a valid updated text, proceed with the update
                                    if (!string.IsNullOrWhiteSpace(updatedText))
                                    {
                                        // Update database
                                        contract.ContractText = updatedText;
                                        _context.Update(contract);
                                        await _context.SaveChangesAsync();
                                        
                                        // Re-extract contract data
                                        await ExtractContractDataAsync(contract, updatedText);
                                        
                                        return new {
                                            prompt_type = promptType,
                                            prompt_response = promptResponse,
                                            updated_text = updatedText
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception fallbackEx) {
                    _logger.LogError(fallbackEx, "Fallback JSON extraction failed");
                }
            }
            
            // Existing fallback for product changes
            if (prompt.Contains("Change the product to", StringComparison.OrdinalIgnoreCase) || 
                prompt.Contains("Update the product to", StringComparison.OrdinalIgnoreCase))
            {
                // Extract the product name from the prompt
                var productMatch = Regex.Match(prompt, @"(?:Change|Update) the product to\s+(.+?)(?:\.|\s*$)", RegexOptions.IgnoreCase);
                if (productMatch.Success)
                {
                    var newProduct = productMatch.Groups[1].Value.Trim();
                    
                    // Update the contract with the new product
                    contract.Product = newProduct;
                    _context.Update(contract);
                    await _context.SaveChangesAsync();
                    
                    return new {
                        prompt_type = "contract_edit",
                        prompt_response = $"The product has been updated to {newProduct}.",
                        updated_text = contract.ContractText.Replace(contract.Product, newProduct)
                    };
                }
            }
            
            throw new Exception($"Error processing GPT response: {ex.Message}. Raw response: {gptResponse}");
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

    private async Task<string> GetOpenAIResponseAsync(object requestBody)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
        
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // Log token usage if available
            if (jsonResponse.TryGetProperty("usage", out var usageElement))
            {
                var promptTokens = usageElement.GetProperty("prompt_tokens").GetInt32();
                var completionTokens = usageElement.GetProperty("completion_tokens").GetInt32();
                var totalTokens = usageElement.GetProperty("total_tokens").GetInt32();
                
                _logger.LogInformation(
                    "Token usage - Prompt: {PromptTokens}, Completion: {CompletionTokens}, Total: {TotalTokens}",
                    promptTokens, completionTokens, totalTokens
                );
            }
            
            return jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error calling OpenAI API: {StatusCode}. Response content: {ErrorContent}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Error: {response.StatusCode}, {errorContent}");
        }
    }

    private string SanitizeJsonResponse(string response)
    {
        // Remove markdown formatting
        response = RemoveMarkdownFormatting(response);
        
        // Handle triple quotes
        response = Regex.Replace(response, @"(?<=""[^""]*""\s*:\s*)""{3}", "\"", RegexOptions.Singleline);
        response = Regex.Replace(response, @"""{3}(?=\s*[,}])", "\"", RegexOptions.Singleline);
        
        // Extract JSON object
        var jsonMatch = Regex.Match(response, @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\}", RegexOptions.Singleline);
        if (jsonMatch.Success)
        {
            response = jsonMatch.Value;
        }
        
        // Escape special characters in string values
        response = Regex.Replace(response, @"(?<=""[^""]*""\s*:\s*"")(.*?)(?="")", 
            m => m.Value
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r"), 
            RegexOptions.Singleline);
        
        return response;
    }
}