#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Lokad.Cloud.Storage
{
    /// <summary>Helper extensions methods for storage providers.</summary>
    public static class QueueStorageExtensions
    {
        /// <summary>Gets messages from a queue with a visibility timeout of 2 hours and a maximum of 50 processing trials.</summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="queueName">Identifier of the queue to be pulled.</param>
        /// <param name="count">Maximal number of messages to be retrieved.</param>
        /// <param name="provider">Provider for the queue storage.</param>
        /// <returns>Enumeration of messages, possibly empty.</returns>
        public static IEnumerable<T> Get<T>(this IQueueStorageProvider provider, string queueName, int count)
        {
            return provider.Get<T>(queueName, count, new TimeSpan(2, 0, 0), 5);
        }

        /// <summary>Gets messages from a queue with a visibility timeout of 2 hours.</summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="provider">Queue storage provider.</param>
        /// <param name="queueName">Identifier of the queue to be pulled.</param>
        /// <param name="count">Maximal number of messages to be retrieved.</param>
        /// <param name="maxProcessingTrials">
        /// Maximum number of message processing trials, before the message is considered as
        /// being poisonous, removed from the queue and persisted to the 'failing-messages' store.
        /// </param>
        /// <returns>Enumeration of messages, possibly empty.</returns>
        public static IEnumerable<T> Get<T>(this IQueueStorageProvider provider, string queueName, int count, int maxProcessingTrials)
        {
            return provider.Get<T>(queueName, count, new TimeSpan(2, 0, 0), maxProcessingTrials);
        }

        /// <summary>Gets messages from a queue (derived from the message type T).</summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="count">Maximal number of messages to be retrieved.</param>
        /// <param name="visibilityTimeout">
        /// The visibility timeout, indicating when the not yet deleted message should
        /// become visible in the queue again.
        /// </param>
        /// <param name="maxProcessingTrials">
        /// Maximum number of message processing trials, before the message is considered as
        /// being poisonous, removed from the queue and persisted to the 'failing-messages' store.
        /// </param>
        /// <returns>Enumeration of messages, possibly empty.</returns>
        public static IEnumerable<T> Get<T>(this IQueueStorageProvider provider, int count, TimeSpan visibilityTimeout, int maxProcessingTrials)
        {
            return provider.Get<T>(GetDefaultStorageName(typeof(T)), count, visibilityTimeout, maxProcessingTrials);
        }

        /// <summary>Gets messages from a queue (derived from the message type T) with a visibility timeout of 2 hours and a maximum of 50 processing trials.</summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="count">Maximal number of messages to be retrieved.</param>
        /// <param name="provider">Provider for the queue storage.</param>
        /// <returns>Enumeration of messages, possibly empty.</returns>
        public static IEnumerable<T> Get<T>(this IQueueStorageProvider provider, int count)
        {
            return provider.Get<T>(GetDefaultStorageName(typeof(T)), count, new TimeSpan(2, 0, 0), 5);
        }

        /// <summary>Gets messages from a queue (derived from the message type T) with a visibility timeout of 2 hours.</summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="provider">Queue storage provider.</param>
        /// <param name="count">Maximal number of messages to be retrieved.</param>
        /// <param name="maxProcessingTrials">
        /// Maximum number of message processing trials, before the message is considered as
        /// being poisonous, removed from the queue and persisted to the 'failing-messages' store.
        /// </param>
        /// <returns>Enumeration of messages, possibly empty.</returns>
        public static IEnumerable<T> Get<T>(this IQueueStorageProvider provider, int count, int maxProcessingTrials)
        {
            return provider.Get<T>(GetDefaultStorageName(typeof(T)), count, new TimeSpan(2, 0, 0), maxProcessingTrials);
        }

        /// <summary>Put a message on a queue (derived from the message type T).</summary>
        public static void Put<T>(this IQueueStorageProvider provider, T message)
        {
            provider.Put(GetDefaultStorageName(typeof(T)), message);
        }

        /// <summary>Put messages on a queue (derived from the message type T).</summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="messages">Messages to be put.</param>
        /// <remarks>If the queue does not exist, it gets created.</remarks>
        public static void PutRange<T>(this IQueueStorageProvider provider, IEnumerable<T> messages)
        {
            provider.PutRange(GetDefaultStorageName(typeof(T)), messages);
        }

        public static string GetDefaultStorageName(Type type)
        {
            var name = type.FullName.ToLowerInvariant().Replace(".", "-");

            // TODO: need a smarter behavior with long type name.
            if (name.Length > 63)
            {
                throw new ArgumentOutOfRangeException("type", "Type name is too long for auto-naming.");
            }

            return name;
        }
    }
}
