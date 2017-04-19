﻿namespace ReadModel
{
    using System;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using EventHandlers;
    using Microsoft.Extensions.DependencyInjection;
    using MongoDB.Driver;
    using Newtonsoft.Json;
    using ParalectEventSourcing.Dispatching;
    using ParalectEventSourcing.Messaging.RabbitMq;
    using ParalectEventSourcing.Persistence.MongoDb;
    using ParalectEventSourcing.Serialization;
    using RabbitMQ.Client.Events;
    using Serilog;

    public class Program
    {
        private static IServiceProvider _serviceProvider;

        public static void Main(string[] args)
        {
            RegisterDependencies();
            ListenToMessages();

            Console.ReadLine();
        }

        private static void RegisterDependencies()
        {
            var dispatcherConfiguration = new DispatcherConfiguration();

            var mongoDbConnectionSettings = new MongoDbConnectionSettings();
            var mongoClient = new MongoClient(new MongoClientSettings
            {
                Server = new MongoServerAddress(mongoDbConnectionSettings.HostName, mongoDbConnectionSettings.Port)
            });

            _serviceProvider = new ServiceCollection()

                // TODO consider creating channels per thread
                .AddSingleton<IChannel, Channel>()
                .AddSingleton<IReadModelChannel, Channel>()
                .AddSingleton<RabbitMqConnectionSettings>(new RabbitMqConnectionSettings())
                .AddSingleton<IChannelFactory, ChannelFactory>()
                .AddTransient<IMessageSerializer, DefaultMessageSerializer>()

                .AddTransient<IDispatcher, EventDispatcher>()

                .AddSingleton<DeviceEventsHandler, DeviceEventsHandler>()
                .AddSingleton<ShipmentEventsHandler, ShipmentEventsHandler>()

                .AddSingleton<DispatcherConfiguration>(dispatcherConfiguration)
                .AddSingleton<ILogger>(Log.Logger)

                .AddSingleton<IMongoClient>(mongoClient)
                .AddTransient<IDatabase, Database>()

                .BuildServiceProvider();

            dispatcherConfiguration.ServiceLocator = _serviceProvider;

            dispatcherConfiguration
                .DispatcherEventHandlerRegistry
                .Register(Assembly.GetEntryAssembly(), new[] { typeof(DeviceEventsHandler).Namespace });
        }

        private static void ListenToMessages()
        {
            Task.Run(() =>
            {
                var channel = _serviceProvider.GetService<IReadModelChannel>();
                channel.Subscribe(ExchangeConfiguration.ReadModelExchange, ConsumerOnReceived);
            });
        }

        private static void ConsumerOnReceived(object sender, BasicDeliverEventArgs basicDeliverEventArgs)
        {
            var messageSerializer = _serviceProvider.GetService<IMessageSerializer>();
            var @event = messageSerializer.Deserialize(basicDeliverEventArgs.Body, e => e.Metadata.TypeName);

            var eventDispatcher = _serviceProvider.GetService<IDispatcher>();
            eventDispatcher.Dispatch(@event);

            Console.WriteLine("Event handled successfully.");
        }
    }
}
