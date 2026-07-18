namespace pzellhorn.Core.Messaging
{
    public record QueueDepth(string Queue, uint MessageCount, uint ConsumerCount, bool Exists);

    /// <summary>
    /// Reads runtime state of a named destination without consuming from it.
    /// </summary>
    public interface IQueueInspector
    {
        Task<QueueDepth> GetDepth(string queue, CancellationToken cancellationToken = default);
    }
}
