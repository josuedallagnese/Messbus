namespace MessageBus.PubSub.Tests;

public class ConsumerCollector<TEvent, TConsumer>
    where TConsumer : IMessageConsumer<TEvent>
{
    private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int? FailUntil { get; set; }
    public int AttemptCount { get; set; }

    public bool Success { get; set; }

    public void AddAttempt()
    {
        AttemptCount++;

        if (AttemptCount <= FailUntil)
            throw new Exception("Consumer collector error");
    }

    public Task Successful()
    {
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

    public Task WaitForItAsync(TimeSpan? timeout = null)
    {
        return timeout == null
            ? _tcs.Task
            : _tcs.Task.WaitAsync(timeout.Value);
    }
}
