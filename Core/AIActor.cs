namespace Jason5Lee.WriterTool.Core;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents an AI agent that can interact with a chat completions API.
/// </summary>
public class AIActor
{
    private readonly string? _key;
    private readonly string _model;
    private readonly Uri _completionsEndpoint;

    /// <summary>
    /// Initializes a new instance of the AIActor class.
    /// </summary>
    /// <param name="apiUrl">The base URL of the AI API (e.g., "https://api.openai.com").</param>
    /// <param name="apiKey">The API key for authentication. Can be null if not required.</param>
    /// <param name="model">The specific model to use for completions (e.g., "gpt-4o").</param>
    /// <exception cref="ArgumentNullException">Thrown if apiUrl or model is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the constructed API endpoint URL is invalid.</exception>
    public AIActor(string apiUrl, string? apiKey, string model)
    {
        _key = apiKey;
        _model = model;

        // 2. Verify that the constructed completions URL is a valid HTTP URL.
        var endpointUrl = $"{apiUrl.TrimEnd('/')}/v1/chat/completions";
        if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var validatedUri)
            || (validatedUri.Scheme != Uri.UriSchemeHttp && validatedUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException($"The constructed API URL '{endpointUrl}' is not a valid HTTP/HTTPS URL.", nameof(apiUrl));
        }
        _completionsEndpoint = validatedUri;
    }

    /// <summary>
    /// Calls the AI completions API with a given prompt and returns the response.
    /// It will retry up to 3 times with increasing delays if the call fails.
    /// </summary>
    /// <param name="prompt">The user's prompt to send to the AI.</param>
    /// <returns>The content of the AI's response as a string.</returns>
    /// <exception cref="ApplicationException">Thrown if the API call fails after all retry attempts.</exception>
    public async Task<string> GetCompletionAsync(HttpClient httpClient, string? system, string user)
    {
        var requestPayload = new ChatRequest(
            Model: _model,
            Messages:
            system == null ? [
                new ChatMessage(Role: "user", Content: user)
            ] : [
                new ChatMessage(Role: "system", Content: system),
                new ChatMessage(Role: "user", Content: user)
            ]
        );
        var jsonPayload = JsonSerializer.Serialize(requestPayload, ChatJsonContext.Default.ChatRequest);
        using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage()
        {
            RequestUri = _completionsEndpoint,
            Method = HttpMethod.Post,
            Content = content
        };

        if (!string.IsNullOrWhiteSpace(_key))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _key);
        }

        var response = await httpClient.SendAsync(request);

        // Throws HttpRequestException if the response is not successful
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var chatResponse = JsonSerializer.Deserialize(responseBody, ChatJsonContext.Default.ChatResponse);

        // Return the first valid response content
        var responseContent = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            throw new ApplicationException("Received an empty or invalid response from the AI.");
        }
        return responseContent;
    }
}

// Inner classes for JSON serialization/deserialization for AI Actor
internal record ChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] List<ChatMessage> Messages
);

internal record ChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);

internal record ChatResponse(
    [property: JsonPropertyName("choices")] List<Choice>? Choices
);

internal record Choice(
    [property: JsonPropertyName("message")] ChatMessage? Message
);

// Source generator context
[JsonSerializable(typeof(ChatResponse))]
[JsonSerializable(typeof(ChatRequest))]
[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(Choice))]
internal partial class ChatJsonContext : JsonSerializerContext
{
}