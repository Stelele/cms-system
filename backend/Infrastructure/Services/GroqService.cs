using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class GroqService : IGroqService
{
    private const string GROQ_API_URL = "https://api.groq.com/openai/v1/chat/completions";
    private const string MODEL = "llama-3.3-70b-versatile";
    private const int MAX_TOKENS = 100;

    private static readonly string SystemPrompt = """
        Role:
        You are an expert social media strategist and perceptive copywriter specializing in creating irresistible, magnetic taglines.

        Task:
        Read the provided text and generate a single, highly engaging tagline that summarizes the core message while perfectly mirroring the original text.

        Strict Guidelines:

        Mirror the Mood: Detect the emotional undertone of the post (e.g., sarcastic, enthusiastic, somber, professional, whimsical) and perfectly reflect it in your generated tagline.

        Match the Energy: If the post is loud and fast-paced, use punchy words and dynamic punctuation. If it's calm and reflective, use a measured, thoughtful cadence.

        Adopt the Language: Write the tagline in the exact same language, dialect, and level of formality as the post. If the text uses specific slang, regionalisms, or industry jargon, incorporate that same flavor naturally.

        Capture the Core Content: The tagline must accurately tease the main point of the post without giving away the entire payoff. It should make the reader want to engage.

        Keep it Concise: The tagline must be short, punchy, and under 15 words. Focus on maximum impact with minimal text.

        Output Constraint:
        Output ONLY the tagline. Do not include introductory text, explanations, or quotation marks. Give me the hook and nothing else.
        """;

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GroqService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Groq:ApiKey"]
            ?? Environment.GetEnvironmentVariable("GROQ_API_KEY")
            ?? throw new InvalidOperationException("Groq API key is not configured. Set 'Groq:ApiKey' in appsettings.json or 'GROQ_API_KEY' environment variable.");
    }

    public async Task<string> GenerateSummaryAsync(string content, CancellationToken cancellationToken = default)
    {
        var cleanContent = CleanContent(content);

        var requestBody = new
        {
            model = MODEL,
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = $"Article text:\n\n{cleanContent}" }
            },
            max_completion_tokens = MAX_TOKENS
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, GROQ_API_URL);
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var summary = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        return summary.Trim();
    }

    private static string CleanContent(string content)
    {
        var result = content;
        foreach (var c in new[] { '#', '*', '_', '`', '~', '[', ']' })
        {
            result = result.Replace(c, ' ');
        }
        return result.Replace('\n', ' ').Trim();
    }
}
