using System;
using System.Collections.Generic;
using System.Text;

namespace pzellhorn.Core.Messaging
{
    /// <summary>
    /// Subscribes to a channel.
    /// Dispose the returned handler to stop consuming.
    /// </summary>
    public interface IQueueConsumer
    {
        Task<IAsyncDisposable> Subscribe<T>(string source, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default);
    }
}
