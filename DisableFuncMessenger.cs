using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FuncSbProxy;


/// <summary>
/// Helper class to send messages to an Azure Storage Queue to disable this function app trigger
/// </summary>
public class DisableFuncMessenger
{
    private readonly ILogger<DisableFuncMessenger> _logger;
    private readonly IConfiguration _configuration;
    
    public DisableFuncMessenger(ILogger<DisableFuncMessenger> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }


    /// <summary>
    /// Sends a function control message to an Azure Storage Queue
    /// </summary>
    /// <param name="functionAppName">Name of the Azure Function App</param>
    /// <param name="functionName">Name of the specific function</param>
    /// <param name="resourceGroupName">Name of the resource group containing the function app</param>
    /// <param name="disablePeriodMinutes">How many minutes to disable the function for</param>
    /// <returns>A boolean indicating if the message was sent successfully</returns>
    public async Task<bool> DisableFuncAsync( string functionAppName,string functionName,  int disablePeriodMinutes = 5)
    {
        var resourceGroupName = _configuration["ResourceGroupName"]
            ?? throw new InvalidOperationException("ResourceGroupName configuration is missing");

        // Create the message object with the necessary details
        var message = new
        {
            FunctionAppName = functionAppName,
            FunctionName = functionName,
            ResourceGroupName = resourceGroupName,
            DisableFunction = true,
            DisablePeriodMinutes = disablePeriodMinutes
        };

        try
        {
            // Serialize the message to JSON
            string jsonMessage = JsonSerializer.Serialize(message);
            // Send the message to the configured queue
            return await SendMessageAsync(jsonMessage);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error occurred while serializing function control message to JSON");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending function control message");
            return false;
        }
        
    }



    /// <summary>
    /// Sends a message to an Azure Storage Queue
    /// </summary>
    /// <param name="queueName">Name of the queue to send the message to</param>
    /// <param name="message">The message content to send</param>
    /// <param name="connectionString">Optional connection string override (if not provided, uses "StorageQueueConnection" from configuration)</param>
    /// <returns>A boolean indicating if the message was sent successfully</returns>
    private async Task<bool> SendMessageAsync(string message)
    {

        // Get connection string from configuration if not provided
        var storageConnString = _configuration["StorageQueueConnection"]
            ?? throw new InvalidOperationException("Storage Queue connection string is missing");

        var queueName = _configuration["StorageQueueName"] ?? throw new InvalidOperationException("Queue name is missing");

        try
        {

            // Create a client and ensure the queue exists
            QueueClient queueClient = new QueueClient(storageConnString, queueName);
            await queueClient.CreateIfNotExistsAsync();

            _logger.LogInformation("Sending message to queue: {QueueName}", queueName);

            // Convert message to base64 to handle special characters properly
            string base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(message));

            // Send the message
            var response = await queueClient.SendMessageAsync(base64Message);

            if (response != null && response.Value != null)
            {
                _logger.LogInformation("Message sent successfully to queue {QueueName}, MessageId: {MessageId}",
                    queueName, response.Value.MessageId);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to send message to queue {QueueName}", queueName);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending message to queue {QueueName}", queueName);
            return false;
        }
    }





}

