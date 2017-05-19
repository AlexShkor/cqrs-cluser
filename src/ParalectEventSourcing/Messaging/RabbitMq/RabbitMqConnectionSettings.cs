﻿namespace ParalectEventSourcing.Messaging.RabbitMq
{
    public class RabbitMqConnectionSettings
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }

        public RabbitMqConnectionSettings()
        {
            UserName = "guest";
            Password = "guest";
            VirtualHost = "/";
            HostName = "rabbitmq";
            Port = 5672;
        }
    }
}
