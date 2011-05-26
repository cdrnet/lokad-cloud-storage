using System;

namespace Lokad.Cloud.Storage.Events.Observers
{
    public class CloudStorageSystemObserver : IDisposable, ICloudStorageSystemObserver
    {
        readonly IObserver<ICloudStorageEvent>[] _observers;

        public CloudStorageSystemObserver(IObserver<ICloudStorageEvent>[] observers)
        {
            _observers = observers;
        }

        public void Notify(ICloudStorageEvent @event)
        {
            // NOTE: Assuming event observers are light - else we may want to do this async
            foreach (var observer in _observers)
            {
                observer.OnNext(@event);
            }
        }

        public void Dispose()
        {
            foreach (var observer in _observers)
            {
                observer.OnCompleted();
            }
        }
    }
}
