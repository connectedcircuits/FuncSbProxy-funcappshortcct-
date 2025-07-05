# FuncSbProxy

## Overview
This project demonstrates an Azure Function App that acts as a proxy between Azure Service Bus and HTTP endpoints. It shows how to implement the short circuit pattern using the FuncTriggerManager untility (https://github.com/connectedcircuits/funcappshortcct) to temporarily disable a function trigger when errors occur.

## Key Features
- Receives messages from Azure Service Bus queues
- Forwards messages to HTTP endpoints
- Implements error handling and retry logic
- **Demonstrates the FuncTriggerManager pattern** for dynamically enabling/disabling function triggers when downstream systems are unavailable

## How It Works
1. The `Function1` class processes messages from a Service Bus queue
2. Messages are forwarded to an HTTP endpoint using the `HttpMessageSender`
3. On failure, the `DisableFuncMessenger` sends a control message to temporarily disable the function trigger
4. This prevents overwhelming downstream systems that are experiencing issues and does not deadletter messages whilst the down stream system is done by trying to process them from the service bus queue.

## Configuration Settings
- `HttpEndpoint`: Target endpoint for HTTP requests
- `ServiceBusConnection`: Connection string for Azure Service Bus
- `StorageQueueConnection`: Connection string for Azure Storage
- `StorageQueueName`: Queue name for function control messages
- `ResourceGroupName`: Azure Resource Group containing the function app
- `DisableFuncPeriodMin`: Duration in minutes to disable the function when errors occur

## Example Use Case
This solution serves as a practical example of how to implement the FuncTriggerManager pattern to improve resilience in serverless architectures. By temporarily disabling triggers when downstream systems fail, you can prevent excessive retry attempts and reduce costs.

## Getting Started
1. Clone this repository
2. Update the connection strings in `local.settings.json`
3. Run the function locally using the Azure Functions Core Tools
