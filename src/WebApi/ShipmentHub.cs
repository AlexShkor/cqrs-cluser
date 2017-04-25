﻿namespace WebApi
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Contracts.Events;
    using Microsoft.AspNetCore.SignalR;
    using ParalectEventSourcing.Messaging.RabbitMq;
    using ParalectEventSourcing.Serialization;
    using RabbitMQ.Client.Events;

    public class ShipmentHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> SuccessConsumers = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, string> ErrorConsumers = new ConcurrentDictionary<string, string>();

        private readonly IMessageSerializer _messageSerializer;
        private readonly ISuccessChannel _successChannel;
        private readonly IErrorChannel _errorChannel;

        public ShipmentHub(ISuccessChannel successChannel, IErrorChannel errorChannel, IMessageSerializer messageSerializer)
        {
            _messageSerializer = messageSerializer;
            _successChannel = successChannel;
            _errorChannel = errorChannel;
        }

        public void Listen(string connectionId)
        {
            Task.Run(() =>
            {
                var successConsumerTag =_successChannel.SubscribeToExchange(RabbitMqRoutingConfiguration.SuccessExchange, connectionId, ConsumerOnSuccess);
                SuccessConsumers.TryAdd(connectionId, successConsumerTag);
            });

            Task.Run(() =>
            {
                var errorConsumerTag = _errorChannel.SubscribeToExchange(RabbitMqRoutingConfiguration.ErrorExchange, connectionId, ConsumerOnError);
                ErrorConsumers.TryAdd(connectionId, errorConsumerTag);
            });
        }

        private void ConsumerOnSuccess(object sender, BasicDeliverEventArgs basicDeliverEventArgs)
        {
            var @event = _messageSerializer.Deserialize(basicDeliverEventArgs.Body, e => e.Metadata.TypeName);

            var connectionId = (string) @event.Metadata.ConnectionId;
            var eventType = Type.GetType(@event.Metadata.TypeName);

            // TODO dispatch events
            if (eventType == typeof(ShipmentCreated))
            {
                Clients.Client(connectionId).shipmentCreated(@event.Id, @event.Address);
            }
            else if (eventType == typeof(ShipmentAddressChanged))
            {
                Clients.Client(connectionId).shipmentAddressChanged(@event.Id, @event.NewAddress);
            }
        }

        private void ConsumerOnError(object sender, BasicDeliverEventArgs e)
        {
            var message = _messageSerializer.Deserialize(e.Body);

            var connectionId = (string) message.OriginalCommand.Metadata.ConnectionId;

            Clients.Client(connectionId).showErrorMessage(message.ErrorMessage);
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            var connectionId = Context.ConnectionId;
            string successConsumerTag;
            SuccessConsumers.TryRemove(connectionId, out successConsumerTag);
            string errorConsumerTag;
            ErrorConsumers.TryRemove(connectionId, out errorConsumerTag);

            _successChannel.Unsubscribe(successConsumerTag);
            _errorChannel.Unsubscribe(errorConsumerTag);

            return base.OnDisconnected(stopCalled);
        }
    }
}
