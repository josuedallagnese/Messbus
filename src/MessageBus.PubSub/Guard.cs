namespace MessageBus.PubSub;

internal static class Guard
{
    public static void ThrowIfNullOrWhiteSpace(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", paramName);
    }
}
