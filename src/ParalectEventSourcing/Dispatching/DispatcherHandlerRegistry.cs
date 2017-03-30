﻿// <copyright file="DispatcherHandlerRegistry.cs" company="Advanced Metering Services LLC">
//     Copyright (c) Advanced Metering Services LLC. All rights reserved.
// </copyright>

namespace ParalectEventSourcing.Dispatching
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Events;
    using Serilog;
    using Snapshoting;

    /// <summary>
    /// Registry of subcriptions to use in the dispatcher
    /// </summary>
    public class DispatcherHandlerRegistry : IDispatcherHandlerRegistry
    {
        /// <summary>
        /// Method name to be used as entry point in handler
        /// </summary>
        public const string HandleMethodName = "Handle";

        private readonly Type markerInterfaceGeneric = typeof(IMessageHandler<>);

        private readonly Type markerInterface = typeof(IMessageHandler);

        private readonly ILogger logger = Log.Logger;

        /// <summary>
        /// Message type -> List of handlers type
        /// </summary>
        private readonly Dictionary<Type, List<Subscription>> subscription = new Dictionary<Type, List<Subscription>>();

        private readonly IHandlerMethodCache handlerMethodCache = new HandlerMethodCache();

        /// <summary>
        /// Register all handlers in assembly (you can register handlers that optionally belongs to specified namespaces)
        /// </summary>
        /// <param name="assembly">assembly to scan</param>
        /// <param name="namespaces">namespaces to scan</param>
        public void Register(Assembly assembly, string[] namespaces)
        {
            this.Register(assembly.GetTypes().Where(t => this.BelongToNamespaces(t, namespaces)));
        }

        /// <summary>
        /// Register all handlers in assemblies (you can register handlers that optionally belongs to specified namespaces)
        /// </summary>
        /// <param name="assemblies">assemblies to scan</param>
        /// <param name="namespaces">namespaces to scan</param>
        public void Register(Assembly[] assemblies, string[] namespaces)
        {
            foreach (var assembly in assemblies)
            {
                this.Register(assembly, namespaces);
            }
        }

        /// <summary>
        ///  Register all handlers in types
        /// </summary>
        /// <param name="types">the types to register</param>
        public void Register(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                this.Register(type);
            }
        }

        /// <summary>
        /// Registers specifc type
        /// </summary>
        /// <param name="type">register the type</param>
        public void Register(Type type)
        {
            var searchTarget = this.markerInterfaceGeneric;

            //var priorityAttribute = ReflectionUtils.GetSingleAttribute<PriorityAttribute>(type); TODO make it work
            PriorityAttribute priorityAttribute = null;

            var defaultPriority = priorityAttribute == null ? 0 : priorityAttribute.Priority;

            var interfaces = type.GetInterfaces();
            var markerInterface = interfaces.FirstOrDefault(i => i == this.markerInterface);
            var markerInterfacesGeneric = interfaces.Where(i => 
                i.GetTypeInfo().IsGenericType 
                && (i.GetGenericTypeDefinition() == searchTarget) 
                && !i.GetTypeInfo().ContainsGenericParameters).ToList();

            if (markerInterface != null)
            {
                var methods = type.GetMethods()
                    .Select(m => new { Method = m, Parameters = m.GetParameters() })
                    .Where(m => m.Method.ReturnType == typeof(void) && m.Parameters.Count() == 1);

                foreach (var method in methods)
                {
                    if (method.Method.Name != HandleMethodName)
                    {
                        throw new Exception(string.Format(
                            "Handler '{0}' has incorrect name of method '{1}' for handling message '{2}'. Change method name to '{3}' or make it not-public (change return type, parameters count, make class not-inhereted from {4}, etc.) to prevent it from message handling.",
                            type,
                            method.Method.Name,
                            method.Parameters.First().ParameterType,
                            HandleMethodName,
                            markerInterface));
                    }

                    var methodAttribute = ReflectionUtils.GetSingleAttribute<PriorityAttribute>(method.Method);
                    var finalPriority = methodAttribute == null ? defaultPriority : methodAttribute.Priority;

                    if (method.Parameters.Count() != 1)
                    {
                        continue;
                    }

                    var messageType = method.Parameters[0].ParameterType;
                    this.AddSubscription(messageType, type, finalPriority);
                }
            }
            else if (markerInterfacesGeneric.Count > 0)
            {
                // for generic marker interfaces [Priority] attribute not supported for methods, but supported for class
                foreach (var i in markerInterfacesGeneric)
                {
                    var messageType = i.GetGenericArguments()[0];
                    this.AddSubscription(messageType, type, defaultPriority);
                }
            }
        }

        /// <summary>
        /// Insures order of handlers
        /// </summary>
        public void InsureOrderOfHandlers()
        {
            foreach (var type in this.subscription.Keys)
            {
                var handlerTypes = this.subscription[type];
                this.SortInPlace(handlerTypes);
            }
        }

        /// <summary>
        /// Sorts subscriptions
        /// </summary>
        /// <param name="list">subscriptions to sort</param>
        public void SortInPlace(List<Subscription> list)
        {
            list.Sort((sub1, sub2) =>
            {
                if (sub1.Priority == sub2.Priority)
                {
                    return 0;
                }

                return (sub1.Priority < sub2.Priority) ? -1 : 1;
            });
        }

        /// <summary>
        /// Gets subscriptions for specific message type
        /// </summary>
        /// <param name="messageType">the message type</param>
        /// <returns>list of subscriptions</returns>
        public List<Subscription> GetSubscriptions(Type messageType)
        {
            if (!this.subscription.ContainsKey(messageType))
            {
                return new List<Subscription>();
            }

            var handlers = this.subscription[messageType];

            if (handlers.Count < 1)
            {
                string errorMessage = string.Format("Handler for type {0} doesn't found.", messageType.FullName);
                throw new Exception(errorMessage);
            }

            return handlers;
        }

        private void AddSubscription(Type messageType, Type handlerType, int priority)
        {
            if (!this.subscription.ContainsKey(messageType))
            {
                this.subscription[messageType] = new List<Subscription>();
            }

            var subscription = new Subscription(handlerType, messageType, this.handlerMethodCache, priority);

            if (!this.subscription[messageType].Contains(subscription))
            {
                this.subscription[messageType].Add(subscription);
            }
        }

        private bool BelongToNamespaces(Type type, string[] namespaces)
        {
            // if no namespaces specified - then type belong to any namespace
            if (namespaces.Length == 0)
            {
                return true;
            }

            foreach (var ns in namespaces)
            {
                if (type.FullName.StartsWith(ns))
                {
                    return true;
                }
            }

            return false;
        }
    }
}