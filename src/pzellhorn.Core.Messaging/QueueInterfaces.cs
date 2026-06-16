namespace pzellhorn.Core.Messaging
{
    /// <summary>
    /// Publishes messages to a named destination (queue/topic). 
    /// </summary>
    public interface IQueuePublisher
    {
        Task Publish<T>(string destination, T message, CancellationToken cancellationToken = default);
    } 

    /// <summary>
    /// Subscribes to a channel.
    /// Dispose the returned handler to stop consuming.
    /// </summary>
    public interface IQueueConsumer
    {
        Task<IAsyncDisposable> Subscribe<T>(string source, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default);
    }
}
