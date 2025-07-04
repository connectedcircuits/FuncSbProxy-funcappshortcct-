using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FuncSbProxy;

public class HttpMessageSender
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpMessageSender> _logger;

    public HttpMessageSender(HttpClient httpClient, ILogger<HttpMessageSender> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a message to the specified HTTP endpoint
    /// </summary>
    /// <param name="endpoint">The URL of the endpoint to send the message to</param>
    /// <param name="message">The message content to send</param>
    /// <param name="messageId">Optional message ID for tracking</param>
    /// <param name="contentType">Optional content type (defaults to application/json)</param>
    /// <returns>The HTTP status code of the response, or 0 if an exception occurred</returns>
    public async Task<int> SendMessageAsync(
        string endpoint,
        string message,
        string? messageId = null,
        string contentType = "application/json")
    {

        try
        {
            _logger.LogInformation("Sending message to endpoint: {Endpoint}", endpoint);
            
            var content = new StringContent(message, Encoding.UTF8, contentType);
            
            // Add messageId to headers if provided
            if (!string.IsNullOrEmpty(messageId))
            {
                content.Headers.Add("X-Message-ID", messageId);
            }

            var response = await _httpClient.PostAsync(endpoint, content);
            int statusCode = (int)response.StatusCode;
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Message sent successfully to {Endpoint}. Status: {StatusCode}", 
                    endpoint, statusCode);
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send message to {Endpoint}. Status: {StatusCode}, Response: {Response}", 
                    endpoint, statusCode, responseBody);
            }
            
            return statusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending message to {Endpoint}", endpoint);
            return 0; // Return 0 to indicate an exception occurred
        }
    }

    /// <summary>
    /// Sends an object as JSON to the specified HTTP endpoint
    /// </summary>
    /// <typeparam name="T">The type of the object to send</typeparam>
    /// <param name="endpoint">The URL of the endpoint to send the message to</param>
    /// <param name="data">The object to serialize and send as JSON</param>
    /// <param name="messageId">Optional message ID for tracking</param>
    /// <returns>The HTTP status code of the response, or 0 if an exception occurred</returns>
    public async Task<int> SendJsonAsync<T>(string endpoint, T data, string? messageId = null)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        try
        {
            string jsonMessage = JsonSerializer.Serialize(data);
            return await SendMessageAsync(endpoint, jsonMessage, messageId, "application/json");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error occurred while serializing object to JSON");
            return 0; // Return 0 to indicate an exception occurred during serialization
        }
    }
}
