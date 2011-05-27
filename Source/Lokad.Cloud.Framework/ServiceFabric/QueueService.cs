#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;

namespace Lokad.Cloud.ServiceFabric
{
    /// <summary>Strongly-type queue service (inheritors are instantiated by
    /// reflection on the cloud).</summary>
    /// <typeparam name="T">Message type</typeparam>
    /// <remarks>
    /// <para>The implementation is not constrained by the 8kb limit for <c>T</c> instances.
    /// If the instances are larger, the framework will wrap them into the cloud storage.</para>
    /// <para>Whenever possible, we suggest to design the service logic to be idempotent
    /// in order to make the service reliable and ultimately consistent.</para>
    /// <para>A empty constructor is needed for instantiation through reflection.</para>
    /// </remarks>
    public abstract class QueueService<T> : CloudService
    {
        readonly string _queueName;
        readonly string _serviceName;
        readonly TimeSpan _visibilityTimeout;
        readonly int _maxProcessingTrials;

        /// <summary>Name of the queue associated to the service.</summary>
        public override string Name
        {
            get { return _serviceName; }
        }

        /// <summary>Default constructor</summary>
        protected QueueService()
        {
            var settings = GetType().GetCustomAttributes(typeof(QueueServiceSettingsAttribute), true)
                                    .FirstOrDefault() as QueueServiceSettingsAttribute;

            // default settings
            _maxProcessingTrials = 5;

            if (null != settings) // settings are provided through custom attribute
            {
                _queueName = settings.QueueName ?? TypeMapper.GetStorageName(typeof (T));
                _serviceName = settings.ServiceName ?? GetType().FullName;

                if (settings.MaxProcessingTrials > 0)
                {
                    _maxProcessingTrials = settings.MaxProcessingTrials;
                }
            }
            else
            {
                _queueName = TypeMapper.GetStorageName(typeof (T));
                _serviceName = GetType().FullName;
            }

            // 1.25 * execution timeout, but limited to 2h max
            _visibilityTimeout = TimeSpan.FromSeconds(Math.Max(1, Math.Min(7200, (1.25*ExecutionTimeout.TotalSeconds))));
        }

        /// <summary>Do not try to override this method, use <see cref="Start"/> instead.</summary>
        protected sealed override ServiceExecutionFeedback StartImpl()
        {
            // 1 message at most
            var messages = QueueStorage.Get<T>(_queueName, 1, _visibilityTimeout, _maxProcessingTrials);

            if (messages.Any())
            {
                var msg = messages.First();
                Start(msg);

                // Messages might have already been deleted by the 'Start' method.
                // It's OK, 'Delete' is idempotent.
                Delete(msg);

                return ServiceExecutionFeedback.WorkAvailable;
            }

            return ServiceExecutionFeedback.Skipped;
        }

        /// <summary>Method called first by the <c>Lokad.Cloud</c> framework when a message is
        /// available for processing. The message is automatically deleted from the queue
        /// if the method returns (no deletion if an exception is thrown).</summary>
        protected abstract void Start(T message);

        /// <summary>
        /// Delete message retrieved through <see cref="Start"/>.
        /// </summary>
        public void Delete(T message)
        {
            QueueStorage.Delete(message);
        }

        /// <summary>
        /// Abandon a messages retrieved through <see cref="Start"/>
        /// and put it visibly back on the queue.
        /// </summary>
        public void Abandon(T message)
        {
            QueueStorage.Abandon(message);
        }
    }
}
