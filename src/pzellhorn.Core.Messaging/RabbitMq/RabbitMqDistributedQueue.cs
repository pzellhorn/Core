using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace pzellhorn.Core.Messaging.RabbitMq
{ 
    public sealed class RabbitMqDistributedQueue : IQueuePublisher, IQueueConsumer, IAsyncDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
        };

        private readonly ConnectionFactory _factory;
        private readonly SemaphoreSlim _connectionGate = new(1, 1);
        private readonly List<IAsyncDisposable> _subscriptions = new();
        private IConnection? _connection;

        public RabbitMqDistributedQueue(IOptions<RabbitMqOptions> options)
        {
            RabbitMqOptions o = options.Value;
            _factory = new ConnectionFactory
            {
                HostName = o.HostName,
                Port = o.Port,
                UserName = o.UserName,
                Password = o.Password,
                VirtualHost = o.VirtualHost,
            };
        }

        public async Task Publish<T>(string destination, T message, CancellationToken cancellationToken = default)
        {
            IConnection connection = await GetConnectionAsync(cancellationToken);
            await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: destination, durable: true, exclusive: false, autoDelete: false,
                cancellationToken: cancellationToken);

            byte[] body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);

            BasicProperties props = new()
            {
                Persistent = true,
                ContentType = "application/json",
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: destination,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken);
        }

        public async Task<IAsyncDisposable> Subscribe<T>(
            string source,
            Func<T, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default)
        {
            IConnection connection = await GetConnectionAsync(cancellationToken);
            IChannel channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: source, 
                durable: true, 
                exclusive: false, 
                autoDelete: false,
                cancellationToken: cancellationToken);

            AsyncEventingBasicConsumer consumer = new(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    T? message = JsonSerializer.Deserialize<T>(ea.Body.Span, JsonOptions);
                    if (message is not null)
                        await handler(message, cancellationToken);

                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
                }
                catch
                { 
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken);
                }
            };

            string consumerTag = await channel.BasicConsumeAsync(
                queue: source, autoAck: false, consumer: consumer,
                cancellationToken: cancellationToken);

            Subscription subscription = new(channel, consumerTag);
            _subscriptions.Add(subscription);
            return subscription;
        }

        private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
        {
            if (_connection is { IsOpen: true })
                return _connection;

            await _connectionGate.WaitAsync(cancellationToken);
            try
            {
                if (_connection is { IsOpen: true })
                    return _connection;

                _connection = await _factory.CreateConnectionAsync(cancellationToken);
                return _connection;
            }
            finally
            {
                _connectionGate.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            foreach (IAsyncDisposable subscription in _subscriptions)
                await subscription.DisposeAsync();

            if (_connection is not null)
                await _connection.DisposeAsync();

            _connectionGate.Dispose();
        }

        private sealed class Subscription(IChannel channel, string consumerTag) : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                try { await channel.BasicCancelAsync(consumerTag); }
                catch { }

                await channel.DisposeAsync();
            }
        }
    }
}
