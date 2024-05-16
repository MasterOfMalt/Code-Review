using Api.Models.RPC;
using MessagePack;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;

namespace Api.Client
{
    public class RpcClient : IRpcClient, IDisposable
    {
        private const string QUEUE_NAME = "rpc_queue";

        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly string replyQueueName;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<int>> createCallbackMapper = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<IReadOnlyCollection<Basket>>> getCallbackMapper = new();

        public RpcClient()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };

            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            // declare a server-named queue
            replyQueueName = channel.QueueDeclare().QueueName;
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                if (!createCallbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
                    return;
                var body = ea.Body.ToArray();
                var response = int.Parse(Encoding.UTF8.GetString(body));
                tcs.TrySetResult(response);
            };

            channel.BasicConsume(consumer: consumer,
                                 queue: replyQueueName,
                                 autoAck: true);
        }

        public Task<int> CallCreateBasketAsync(Basket newBasket, CancellationToken cancellationToken = default)
        {
            IBasicProperties props = channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueueName;
            var messageBytes = new ReadOnlyMemory<byte>(MessagePackSerializer.Serialize(newBasket, null, cancellationToken));
            var tcs = new TaskCompletionSource<int>();
            createCallbackMapper.TryAdd(correlationId, tcs);

            channel.BasicPublish(exchange: string.Empty,
                                 routingKey: QUEUE_NAME,
                                 basicProperties: props,
                                 body: messageBytes);

            cancellationToken.Register(() => createCallbackMapper.TryRemove(correlationId, out _));
            return tcs.Task;
        }

        public Task<IReadOnlyCollection<Basket>> CallGetBasketsAsync(string since, CancellationToken cancellationToken = default)
        {
            IBasicProperties props = channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueueName;
            var messageBytes = Encoding.UTF8.GetBytes(since);
            var tcs = new TaskCompletionSource<IReadOnlyCollection<Basket>>();
            getCallbackMapper.TryAdd(correlationId, tcs);

            channel.BasicPublish(exchange: string.Empty,
                                 routingKey: QUEUE_NAME,
                                 basicProperties: props,
                                 body: messageBytes);

            cancellationToken.Register(() => createCallbackMapper.TryRemove(correlationId, out _));
            return tcs.Task;
        }

        public void Dispose()
        {
            // closing a connection will also close all channels on it
            connection.Close();
            GC.SuppressFinalize(this);
        }
    }
}
