﻿namespace ParalectEventSourcing.Messaging.RabbitMq
{
    using System;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using Serialization;

    public class Channel : ISuccessChannel, IErrorChannel, IWriteModelChannel, IReadModelChannel
    {
        private readonly IModel _channel;
        private readonly IMessageSerializer _messageSerializer;

        public Channel(IModel model, IMessageSerializer messageSerializer)
        {
            _channel = model;

            _channel.QueueDeclare(RabbitMqRoutingConfiguration.WriteModelQueue, true, false, false);
            _channel.QueueDeclare(RabbitMqRoutingConfiguration.ReadModelQueue, true, false, false);

            _channel.ExchangeDeclare(RabbitMqRoutingConfiguration.SuccessExchange, "direct", true);
            _channel.ExchangeDeclare(RabbitMqRoutingConfiguration.ErrorExchange, "direct", true);

            _messageSerializer = messageSerializer;
        }

        public void SendToExchange(string exchange, string routingKey, object message)
        {
            _channel.BasicPublish(exchange, routingKey, null, _messageSerializer.Serialize(message));
        }

        public void SendToQueue(string queue, object message)
        {
            _channel.BasicPublish("", queue, null, _messageSerializer.Serialize(message));
        }

        public string SubscribeToExchange(string exchange, string routingKey, EventHandler<BasicDeliverEventArgs> callback)
        {
            var queueName = _channel.QueueDeclare(exchange + "_" + Guid.NewGuid()).QueueName;
            _channel.QueueBind(queueName, exchange, routingKey);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += callback;
            var consumerTag = _channel.BasicConsume(queueName, true, Guid.NewGuid().ToString(), consumer);

            return consumerTag;
        }

        public void SubscribeToQueue(string queue, EventHandler<BasicDeliverEventArgs> callback)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += callback;
            _channel.BasicConsume(queue, true, consumer);
        }

        public void Unsubscribe(string consumerTag)
        {
            _channel.BasicCancel(consumerTag);
        }
    }
}
