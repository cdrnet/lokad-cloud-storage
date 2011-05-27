#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Storage.Events
{
    public class OperationRetriedEvent : ICloudStorageEvent
    {
        public Exception Exception { get; private set; }
        public string Policy { get; private set; }
        public int Trial { get; private set; }
        public TimeSpan Interval { get; private set; }
        public Guid Sequence { get; private set; }

        public OperationRetriedEvent(Exception exception, string policy, int trial, TimeSpan interval, Guid sequence)
        {
            Exception = exception;
            Policy = policy;
            Trial = trial;
            Interval = interval;
            Sequence = sequence;
        }
    }
}
