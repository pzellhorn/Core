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
        /// <summary>
        /// Consumes from a queue over default exchange
        /// </summary>
        Task<IAsyncDisposable> Subscribe<T>(string source, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default);

        /// <summary>
        /// Consumes from a queue over a specified exchange & routing key
        /// </summary>
        Task<IAsyncDisposable> Subscribe<T>(string queue,string exchange,string exchangeType,string routingKey,Func<T, CancellationToken, Task> handler,CancellationToken cancellationToken = default);
    }
}
