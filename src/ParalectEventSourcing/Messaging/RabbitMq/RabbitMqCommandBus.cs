﻿namespace ParalectEventSourcing.Messaging.RabbitMq
{
    using Commands;
    using Utils;

    public class RabbitMqCommandBus : CommandBus
    {
        private readonly IWriteModelChannel _channel;

        public RabbitMqCommandBus(IDateTimeProvider dateTimeProvider, IWriteModelChannel channel)
            : base(dateTimeProvider)
        {
            _channel = channel;
        }

        protected override void SendInternal(params ICommand[] commands)
        {
            foreach (var command in commands)
            {
                _channel.SendToQueue(RabbitMqRoutingConfiguration.WriteModelQueue, command);
            }
        }
    }
}
