#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Storage.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever a storage operation is retried.
    /// Useful for analyzing retry policy behavior.
    /// </summary>
    public class StorageOperationRetriedEvent : ICloudStorageEvent
    {
        // TODO (ruegg, 2011-05-27): Drop properties that we don't actually need in practice

        public Exception Exception { get; private set; }
        public string Policy { get; private set; }
        public int Trial { get; private set; }
        public TimeSpan Interval { get; private set; }
        public Guid TrialSequence { get; private set; }

        public StorageOperationRetriedEvent(Exception exception, string policy, int trial, TimeSpan interval, Guid trialSequence)
        {
            Exception = exception;
            Policy = policy;
            Trial = trial;
            Interval = interval;
            TrialSequence = trialSequence;
        }
    }
}
