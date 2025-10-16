# Messbus

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%208.0-512BD4)](https://dotnet.microsoft.com/)

A lightweight, extensible message bus framework for .NET that provides a simple abstraction for publishing and consuming messages across different messaging platforms.

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Installation](#installation)
- [Getting Started](#getting-started)
- [Core Concepts](#core-concepts)
- [Google Cloud Pub/Sub Integration](#google-cloud-pubsub-integration)
- [Configuration](#configuration)
- [Advanced Usage](#advanced-usage)
- [Architecture](#architecture)
- [Contributing](#contributing)
- [License](#license)

## 🎯 Overview

Messbus is a flexible messaging framework designed to simplify the implementation of publish-subscribe patterns in .NET applications. It provides a clean abstraction layer that allows you to switch between different messaging platforms without changing your application code.

Currently supported platforms:
- **Google Cloud Pub/Sub** - Full-featured implementation with managed and unmanaged modes

## ✨ Features

- **Simple API** - Intuitive interfaces for publishing and consuming messages
- **Type-Safe** - Strongly-typed message handling with generic constraints
- **Dependency Injection** - First-class support for Microsoft.Extensions.DependencyInjection
- **Background Processing** - Built on top of `IHostedService` for seamless integration with .NET hosting
- **Automatic Retries** - Configurable retry logic with exponential backoff
- **Dead Letter Queues** - Automatic handling of failed messages
- **Batch Publishing** - Efficient batch message publishing
- **Custom Serialization** - Pluggable serialization with JSON support out of the box
- **Emulator Support** - Local development with Google Cloud Pub/Sub Emulator
- **Resource Management** - Automatic topic and subscription creation (optional)
- **Flow Control** - Configurable message flow control for consumers
- **Verbosity Mode** - Detailed logging for debugging and monitoring

## 📦 Installation

Install the core package:

```bash
dotnet add package Messbus
```

Install the Google Cloud Pub/Sub implementation:

```bash
dotnet add package Messbus.PubSub
```

## 🚀 Getting Started

### Basic Publisher

```csharp
using Messbus;
using Messbus.PubSub;

// Define your message
public class OrderCreatedEvent
{
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Configure services
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPubSub(config =>
{
    config.ProjectId = "my-gcp-project";
    config.Publishing = new PublishingConfiguration
    {
        Prefix = "prod",
        Topics = new List<string> { "orders" }
    };
});

var app = builder.Build();

// Publish a message
app.MapPost("/orders", async (IMessageBus messageBus) =>
{
    var orderEvent = new OrderCreatedEvent
    {
        OrderId = Guid.NewGuid().ToString(),
        Amount = 99.99m,
        CreatedAt = DateTime.UtcNow
    };

    var messageId = await Messbus.Publish("orders", orderEvent);
    return Results.Ok(new { MessageId = messageId });
});

app.Run();
```

### Basic Consumer

```csharp
using Messbus;
using Messbus.PubSub;

// Define your message handler
public class OrderCreatedEventConsumer : IMessageConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedEventConsumer> _logger;

    public OrderCreatedEventConsumer(ILogger<OrderCreatedEventConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Handler(MessageContext<OrderCreatedEvent> context, CancellationToken cancellationToken)
    {
        var order = context.Message;
        _logger.LogInformation("Processing order {OrderId} with amount {Amount}", 
            order.OrderId, order.Amount);

        // Your business logic here
        await ProcessOrderAsync(order, cancellationToken);
    }

    private async Task ProcessOrderAsync(OrderCreatedEvent order, CancellationToken cancellationToken)
    {
        // Implementation
        await Task.CompletedTask;
    }
}

// Configure services
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPubSub(config =>
{
    config.ProjectId = "my-gcp-project";
    config.Subscription = new SubscriptionConfiguration
    {
        Sufix = "order-processor",
        AckDeadlineSeconds = 120,
        MaxDeliveryAttempts = 5
    };
})
.AddConsumer<OrderCreatedEvent, OrderCreatedEventConsumer>("orders");

var host = builder.Build();
await host.RunAsync();
```

## 🧩 Core Concepts

### IMessageBus

The main interface for publishing messages:

```csharp
public interface IMessageBus
{
    Task<string> Publish<T>(string topic, T message, CancellationToken cancellationToken = default)
        where T : class;

    Task<IEnumerable<string>> PublishBatch<T>(string topic, IEnumerable<T> messages, CancellationToken cancellationToken = default)
        where T : class;
}
```

### IMessageConsumer<TEvent>

Interface for implementing message handlers:

```csharp
public interface IMessageConsumer<TEvent>
{
    Task Handler(MessageContext<TEvent> context, CancellationToken cancellationToken = default);
}
```

### MessageContext<T>

Provides context information about the message being processed:

```csharp
public record class MessageContext<T>
{
    public string Id { get; }           // Unique message identifier
    public int Attempt { get; }         // Current delivery attempt number
    public byte[] Data { get; }         // Raw message data
    public T Message { get; }           // Deserialized message
}
```

## ☁️ Google Cloud Pub/Sub Integration

### Configuration Options

#### Managed Mode (Recommended)

In managed mode, the framework automatically creates and manages topics, subscriptions, and dead letter queues:

```csharp
builder.Services.AddPubSub(config =>
{
    config.ProjectId = "my-gcp-project";
    config.JsonCredentials = "{...}"; // Optional: Service account JSON
    
    config.Publishing = new PublishingConfiguration
    {
        Prefix = "prod",
        Topics = new List<string> { "orders", "payments", "notifications" },
        MessageRetentionDurationDays = 7
    };
    
    config.Subscription = new SubscriptionConfiguration
    {
        Sufix = "order-service",
        AckDeadlineSeconds = 120,
        MaxDeliveryAttempts = 5,
        MinBackoffSeconds = 5,
        MaxBackoffSeconds = 600,
        MessageRetentionDurationDays = 7,
        MaxOutstandingElementCount = 1000,
        MaxOutstandingByteCount = 100_000_000
    };
})
.AddConsumer<OrderCreatedEvent, OrderCreatedEventConsumer>("orders")
.AddConsumer<PaymentProcessedEvent, PaymentProcessedEventConsumer>("payments");
```

#### Unmanaged Mode

Use existing topics and subscriptions without automatic resource management:

```csharp
builder.Services.AddPubSub(config =>
{
    config.ProjectId = "my-gcp-project";
    config.ResourceInitialization = ResourceInitialization.None;
})
.AddUnmanagedConsumer<OrderCreatedEvent, OrderCreatedEventConsumer>(
    topic: "orders",
    subscription: "existing-subscription-name");
```

### Dead Letter Queue Support

Automatically handle messages that fail after maximum retry attempts:

```csharp
builder.Services.AddPubSub(configuration)
    .AddConsumer<OrderCreatedEvent, OrderCreatedEventConsumer, OrderCreatedEventDeadLetterConsumer>("orders");
```

The dead letter consumer will receive messages that exceed the `MaxDeliveryAttempts` threshold:

```csharp
public class OrderCreatedEventDeadLetterConsumer : IMessageConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedEventDeadLetterConsumer> _logger;

    public OrderCreatedEventDeadLetterConsumer(ILogger<OrderCreatedEventDeadLetterConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Handler(MessageContext<OrderCreatedEvent> context, CancellationToken cancellationToken)
    {
        _logger.LogError("Message {MessageId} failed after {Attempts} attempts", 
            context.Id, context.Attempt);
        
        // Handle failed message (e.g., store in database, send alert, etc.)
        await StoreFailedMessageAsync(context.Message, cancellationToken);
    }
}
```

### Batch Publishing

Efficiently publish multiple messages at once:

```csharp
var orders = new List<OrderCreatedEvent>
{
    new() { OrderId = "1", Amount = 10.00m },
    new() { OrderId = "2", Amount = 20.00m },
    new() { OrderId = "3", Amount = 30.00m }
};

var messageIds = await Messbus.PublishBatch("orders", orders);
```

### Multiple Pub/Sub Instances

Use named instances to connect to multiple GCP projects:

```csharp
// Configure multiple instances
builder.Services
    .AddPubSub(config =>
    {
        config.Alias = "primary";
        config.ProjectId = "primary-project";
        // ... configuration
    })
    .AddPubSub(config =>
    {
        config.Alias = "secondary";
        config.ProjectId = "secondary-project";
        // ... configuration
    });

// Resolve by alias
var primaryBus = serviceProvider.GetRequiredKeyedService<IMessageBus>("primary");
var secondaryBus = serviceProvider.GetRequiredKeyedService<IMessageBus>("secondary");
```

## ⚙️ Configuration

### Configuration from appsettings.json

```json
{
  "Messbus": {
    "PubSub": {
      "ProjectId": "my-gcp-project",
      "JsonCredentials": "{...}",
      "UseEmulator": false,
      "VerbosityMode": false,
      "ResourceInitialization": "All",
      "Publishing": {
        "Prefix": "prod",
        "MessageRetentionDurationDays": 7,
        "Topics": ["orders", "payments", "notifications"]
      },
      "Subscription": {
        "Sufix": "order-service",
        "MessageRetentionDurationDays": 7,
        "AckDeadlineSeconds": 120,
        "MaxDeliveryAttempts": 5,
        "MinBackoffSeconds": 5,
        "MaxBackoffSeconds": 600,
        "MaxOutstandingElementCount": 1000,
        "MaxOutstandingByteCount": 100000000
      }
    }
  }
}
```

Then configure using:

```csharp
builder.Services.AddPubSub(builder.Configuration);
```

### Configuration Options Explained

#### PubSubConfiguration

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `ProjectId` | `string` | GCP Project ID | **Required** |
| `JsonCredentials` | `string` | Service account JSON credentials | Uses Application Default Credentials |
| `UseEmulator` | `bool` | Use Pub/Sub emulator for local development | `false` |
| `VerbosityMode` | `bool` | Enable detailed logging | `false` |
| `ResourceInitialization` | `enum` | Control automatic resource creation | `All` |
| `Alias` | `string` | Named instance identifier | `null` |

#### ResourceInitialization Options

| Option | Description | Required GCP Permission |
|--------|-------------|------------------------|
| `All` | Create topics and subscriptions automatically | **Pub/Sub Admin** |
| `TopicsOnly` | Only create topics | **Pub/Sub Publisher** |
| `SubscriptionsOnly` | Only create subscriptions | **Pub/Sub Subscriber** |
| `None` | Don't create any resources (unmanaged mode) | Depends on usage (Publisher/Subscriber) |

#### PublishingConfiguration

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `Prefix` | `string` | Prefix for topic names | `null` |
| `Topics` | `List<string>` | List of allowed topics | **Required** |
| `MessageRetentionDurationDays` | `int` | Message retention period | `7` |

#### SubscriptionConfiguration

| Property | Type | Description | Default | Minimum |
|----------|------|-------------|---------|---------|
| `Sufix` | `string` | Suffix for subscription names | **Required** | - |
| `MessageRetentionDurationDays` | `int` | Message retention period | `7` | `7` |
| `AckDeadlineSeconds` | `int` | Acknowledgment deadline | `120` | `30` |
| `MaxDeliveryAttempts` | `int` | Maximum retry attempts | `5` | `5` |
| `MinBackoffSeconds` | `int` | Minimum retry backoff | `5` | `5` |
| `MaxBackoffSeconds` | `int` | Maximum retry backoff | `600` | `600` |
| `MaxOutstandingElementCount` | `long?` | Max unacknowledged messages | `null` | - |
| `MaxOutstandingByteCount` | `long?` | Max unacknowledged bytes | `null` | - |

## 🔧 Advanced Usage

### Custom Serialization

Implement your own serializer:

```csharp
public class ProtobufMessageSerializer : IMessageSerializer
{
    public byte[] Serialize<T>(T obj)
    {
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, obj);
        return stream.ToArray();
    }

    public T Deserialize<T>(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return ProtoBuf.Serializer.Deserialize<T>(stream);
    }
}

// Register custom serializer
builder.Services
    .AddPubSub(configuration)
    .AddSerializer<ProtobufMessageSerializer>();
```

### Scoped Dependencies in Consumers

Consumers are resolved from a scoped service provider, allowing you to use scoped dependencies:

```csharp
public class OrderCreatedEventConsumer : IMessageConsumer<OrderCreatedEvent>
{
    private readonly IOrderRepository _repository;
    private readonly IEmailService _emailService;

    public OrderCreatedEventConsumer(
        IOrderRepository repository,
        IEmailService emailService)
    {
        _repository = repository;
        _emailService = emailService;
    }

    public async Task Handler(MessageContext<OrderCreatedEvent> context, CancellationToken cancellationToken)
    {
        await _repository.SaveOrderAsync(context.Message, cancellationToken);
        await _emailService.SendConfirmationAsync(context.Message, cancellationToken);
    }
}
```

### Error Handling and Retries

Messages are automatically retried based on the `MaxDeliveryAttempts` configuration. You can access the current attempt number:

```csharp
public async Task Handler(MessageContext<OrderCreatedEvent> context, CancellationToken cancellationToken)
{
    _logger.LogInformation("Processing message {MessageId}, attempt {Attempt}", 
        context.Id, context.Attempt);

    if (context.Attempt > 3)
    {
        // Apply different logic for later attempts
        await ProcessWithFallbackAsync(context.Message, cancellationToken);
    }
    else
    {
        await ProcessNormallyAsync(context.Message, cancellationToken);
    }
}
```

### Local Development with Emulator

Use the Google Cloud Pub/Sub Emulator for local development:

1. Start the emulator:
```bash
gcloud beta emulators pubsub start --project=local-project
```

2. Configure your application:
```csharp
builder.Services.AddPubSub(config =>
{
    config.ProjectId = "local-project";
    config.UseEmulator = true;
    // ... rest of configuration
});
```

## 🏗️ Architecture

### Core Components

```
Messbus (Core)
├── IMessageBus              - Publishing interface
├── IMessageConsumer<T>      - Consumer interface
├── IMessageSerializer       - Serialization interface
├── Messbus                  - Abstract base class for publishers
├── MessageConsumer<T>       - Abstract base class for consumers
└── MessageContext<T>        - Message context wrapper

Messbus.PubSub (Implementation)
├── PubSubMessageBus         - Google Cloud Pub/Sub publisher
├── PubSubConsumer<T>        - Google Cloud Pub/Sub consumer
├── Configuration/           - Configuration classes
├── Serialization/           - JSON serializer implementation
└── Internal/                - Internal helpers and utilities
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Development Setup

1. Clone the repository
2. Restore dependencies
```bash
dotnet restore
```

3. Build the solution
```bash
dotnet build
```

4. Run tests
```bash
dotnet test
```

### Guidelines

- Follow the existing code style
- Add unit tests for new features
- Update documentation as needed
- Ensure all tests pass before submitting PR

## 📄 License

This project is licensed under the MIT License - see below for details:

```
MIT License

Copyright (c) 2025 Messbus Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
