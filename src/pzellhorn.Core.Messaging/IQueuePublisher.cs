using System;
using System.Collections.Generic;
using System.Text;

namespace pzellhorn.Core.Messaging
{
    /// <summary>
    /// Publishes messages to a named destination (queue/topic). 
    /// </summary>
    public interface IQueuePublisher
    {
        Task Publish<T>(string destination, T message, CancellationToken cancellationToken = default);
    }
}
