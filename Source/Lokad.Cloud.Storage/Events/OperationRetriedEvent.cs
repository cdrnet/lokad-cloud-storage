using System;

namespace Lokad.Cloud.Storage.Events
{
    public class OperationRetriedEvent : ICloudStorageEvent
    {
        public Exception Exception { get; private set; }
        public string Policy { get; private set; }
        public int Trial { get; private set; }

        public OperationRetriedEvent(Exception exception, string policy, int trial)
        {
            Exception = exception;
            Policy = policy;
            Trial = trial;
        }
    }
}
