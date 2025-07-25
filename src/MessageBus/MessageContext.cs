namespace MessageBus;

public record class MessageContext<T>
{
    public string Id { get; }
    public int Attempt { get; }
    public byte[] Data { get; }
    public T Message { get; private set; }

    public MessageContext(string id, int attempt, byte[] data)
    {
        Id = id;
        Attempt = attempt;
        Data = data;
    }

    public void SetMessage(T message)
    {
        Message = message;
    }
}
