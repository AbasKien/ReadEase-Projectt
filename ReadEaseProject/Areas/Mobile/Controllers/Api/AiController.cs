using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReadEaseProject.Areas.Mobile.Controllers.Api
{
    [Area("Mobile")]
    [Route("Areas/Mobile/api/[controller]")]
    [ApiController]
    public class AiController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<AiController> _logger;

        public AiController(IConfiguration config, IHttpClientFactory httpFactory, ILogger<AiController> logger)
        {
            _config = config;
            _httpFactory = httpFactory;
            _logger = logger;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AiChatRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.UserMessage))
                return BadRequest(new { error = "Missing request data." });

            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Gemini API key is not configured.");
                return StatusCode(500, new { error = "AI service not configured." });
            }

            var systemPrompt = $@"You are an AI reading companion for the book ""{request.BookTitle}"".
The reader is currently on Chapter {request.ChapterNumber}: ""{request.ChapterTitle}"".

Here is the chapter content for context:
---
{request.ChapterContent}
---

{(request.Language == "fil" ? "Respond entirely in Filipino (Tagalog)." : "Respond in English.")}
Be helpful, engaging, and conversational. Keep responses concise (2-4 sentences unless more detail is needed).
Do not spoil future chapters. Focus only on what has been revealed so far.";

            var fullPrompt = $"{systemPrompt}\n\nUser: {request.UserMessage}\nAssistant:";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = fullPrompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 1000
                }
            };

            // ✅ Use camelCase serializer for the REQUEST to Gemini
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            using var client = _httpFactory.CreateClient();
            var json = JsonSerializer.Serialize(payload, jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
                var response = await client.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Gemini response: {Status} - {Body}", response.StatusCode, responseString);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini error: {StatusCode} - {Response}", response.StatusCode, responseString);
                    return StatusCode(500, new { error = "AI service returned an error.", detail = responseString });
                }

                // ✅ Use case-insensitive deserialization for the RESPONSE from Gemini
                var deserializeOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<GeminiResponse>(responseString, deserializeOptions);
                var reply = result?.Candidates?[0]?.Content?.Parts?[0]?.Text
                            ?? "Sorry, I couldn't generate a response.";

                return Ok(new { reply });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while calling Gemini API.");
                return StatusCode(500, new { error = "Something went wrong.", detail = ex.Message });
            }
        }
        [HttpGet("debug")]
        public IActionResult Debug()
        {
            var key = _config["Gemini:ApiKey"];
            return Ok(new
            {
                hasKey = !string.IsNullOrEmpty(key),
                keyPreview = string.IsNullOrEmpty(key) ? "MISSING" : key[..8] + "..."
            });
        }
    }

    public class AiChatRequest
    {
        public string BookTitle { get; set; } = "";
        public int ChapterNumber { get; set; }
        public string ChapterTitle { get; set; } = "";
        public string ChapterContent { get; set; } = "";
        public string UserMessage { get; set; } = "";
        public string Language { get; set; } = "en";
    }

    public class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    public class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    public class GeminiContent
    {
        public List<GeminiPart>? Parts { get; set; }
    }

    public class GeminiPart
    {
        public string? Text { get; set; }
    }
}