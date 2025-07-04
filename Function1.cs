using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FuncSbProxy;

public class Function1
{
    private readonly ILogger<Function1> _logger;
    private readonly HttpMessageSender _httpMessageSender;
    private readonly DisableFuncMessenger _disableFuncMessenger;
    private readonly IConfiguration _configuration;

    public Function1(ILogger<Function1> logger, HttpMessageSender httpMessageSender, DisableFuncMessenger disableFuncMessenger, IConfiguration configuration)
    {
        _logger = logger;
        _httpMessageSender = httpMessageSender;
        _disableFuncMessenger = disableFuncMessenger;
        _configuration = configuration;
    }

    [Function(nameof(Function1))]
    public async Task Run(
        [ServiceBusTrigger("myqueue", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        var functionName = nameof(Function1);

        // Get function app name from configuration or environment
        var functionAppName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") 
            ?? _configuration["FunctionAppName"] 
            ?? "FuncSbProxy";

        // Get the period to disable the function for
        var disablePeriodMinutes = _configuration.GetValue<int>("DisableFuncPeriodMin", 5);

        try
        {
            // Get the HTTP endpoint from configuration
            string httpEndpoint = _configuration["HttpEndpoint"]
                ?? throw new InvalidOperationException("HttpEndpoint configuration is missing");

            // Convert the message body to string
            string messageBody = message.Body.ToString();

            // Send the message to the HTTP endpoint
            int statusCode = await _httpMessageSender.SendMessageAsync(
                httpEndpoint,
                messageBody,
                message.MessageId,
                message.ContentType ?? "application/json");

            // Check if the status code indicates success (2xx range)
            if (statusCode >= 200 && statusCode < 300)
            {
                _logger.LogInformation("Successfully forwarded message {id} to HTTP endpoint. Status: {StatusCode}",
                    message.MessageId, statusCode);
                // Complete the message only if it was successfully sent
                await messageActions.CompleteMessageAsync(message);
            }
            else if (statusCode == 0)
            {
                _logger.LogError("Exception occurred while forwarding message {id} to HTTP endpoint", message.MessageId);
                // Abandon the message to retry later
                await messageActions.AbandonMessageAsync(message);
            }
            else
            {
                _logger.LogWarning("Failed to forward message {id} to HTTP endpoint. Status: {StatusCode}",
                    message.MessageId, statusCode);

                // For certain status codes, we might want to dead-letter instead of retry
                if (statusCode == 400 || statusCode == 422)
                {
                    // Bad request or unprocessable entity - likely won't succeed on retry
                    _logger.LogWarning("Message {id} failed with client error status code. Dead-lettering message.", message.MessageId);
                    // Use the simpler overload of DeadLetterMessageAsync
                    await messageActions.DeadLetterMessageAsync(message);
                }
                else
                {
                    // For server errors (5xx) or other status codes.
                    _logger.LogWarning("Message {id} failed with client error status code {statusCode}. Abandoning message.", message.MessageId, statusCode);
                    // First send a disable the function trigger, than abandon the message to retry later
                    await _disableFuncMessenger.DisableFuncAsync(functionAppName, functionName, disablePeriodMinutes);
                    await messageActions.AbandonMessageAsync(message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {id}", message.MessageId);
            // Abandon the message to retry later
            await messageActions.AbandonMessageAsync(message);
        }
    }
}