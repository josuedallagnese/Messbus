namespace Messbus.PubSub.Tests;

public class ConsumerCollector<TEvent, TConsumer>
    where TConsumer : IMessageConsumer<TEvent>
{
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _messageCount;
    private int _attemptCount;
    private object _event;
    private bool _success;
    
    public int MessageCount => _messageCount;
    public int AttemptCount => _attemptCount;
    public TEvent Event => (TEvent)_event;
    public bool IsCompleted => _tcs.Task.IsCompleted;

    public int? FailUntil { get; set; }
    public int ExpectedMessageCount { get; set; }

    public bool Success
    {
        get => Volatile.Read(ref _success);
        private set => Volatile.Write(ref _success, value);
    }

    public void AddAttempt()
    {
        int newAttempt = Interlocked.Increment(ref _attemptCount);

        if (FailUntil.HasValue && newAttempt <= FailUntil.Value)
            throw new Exception("Consumer collector error");
    }

    public Task AddMessageCount()
    {
        int newCount = Interlocked.Increment(ref _messageCount);

        if (newCount >= ExpectedMessageCount)
            _tcs.TrySetResult();

        return Task.CompletedTask;
    }

    public Task Successful(TEvent @event)
    {
        Interlocked.Exchange(ref _event, @event);
        Success = true;
        _tcs.TrySetResult();
        return Task.CompletedTask;
    }

    public Task Unsuccessful()
    {
        Success = false;
        _tcs.TrySetResult();
        return Task.CompletedTask;
    }

    public Task WaitForItAsync()
    {
        return _tcs.Task;
    }

    public Task WaitForItAsync(TimeSpan timeToWait)
    {
        return Task.WhenAny(_tcs.Task, Task.Delay(timeToWait));
    }

    public void Reset()
    {
        _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _messageCount = 0;
        _attemptCount = 0;
        _event = null;
        _success = false;
    }
}
