namespace Lokad.Cloud.Storage.Events.Observers
{
    public interface ICloudStorageSystemObserver
    {
        void Notify(ICloudStorageEvent @event);
    }
}