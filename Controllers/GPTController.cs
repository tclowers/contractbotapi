using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using OpenAI_API;

namespace GPTWrapper.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GPTController : ControllerBase
    {
        private readonly OpenAIAPI _openAIAPI;

        public GPTController(OpenAIAPI openAIAPI)
        {
            _openAIAPI = openAIAPI;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GPTRequest request)
        {
            var result = await _openAIAPI.Completions.CreateCompletionAsync(request.Prompt);
            return Ok(result);
        }
    }

    public class GPTRequest
    {
        public string? Prompt { get; set; }
    }
}