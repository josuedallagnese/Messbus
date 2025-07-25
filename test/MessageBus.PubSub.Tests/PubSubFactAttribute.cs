using System;
using Xunit;

namespace MessageBus.PubSub.Tests;

/// <summary>
/// Custom fact attribute that skips a test when neither PUBSUB_EMULATOR_HOST
/// nor GOOGLE_APPLICATION_CREDENTIALS environment variables are set.
/// </summary>
public sealed class PubSubFactAttribute : FactAttribute
{
    public PubSubFactAttribute()
    {
        var emulator = Environment.GetEnvironmentVariable("PUBSUB_EMULATOR_HOST");
        var credentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

        if (string.IsNullOrWhiteSpace(emulator) && string.IsNullOrWhiteSpace(credentials))
        {
            Skip = "PubSub emulator or credentials not configured";
        }
    }
}
