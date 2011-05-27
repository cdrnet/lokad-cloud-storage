#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Storage
{
    /// <summary>Storage providers and runtime providers.</summary>
    public class CloudStorageProviders
    {
        /// <summary>Abstracts the Blob Storage.</summary>
        public IBlobStorageProvider BlobStorage { get; private set; }

        /// <summary>Abstracts the Queue Storage.</summary>
        public IQueueStorageProvider QueueStorage { get; private set; }

        /// <summary>Abstracts the Table Storage.</summary>
        public ITableStorageProvider TableStorage { get; private set; }

        /// <summary>Abstracts the finalizer (used for fast resource release
        /// in case of runtime shutdown).</summary>
        public IRuntimeFinalizer RuntimeFinalizer { get; private set; }

        /// <summary>Abstracts the logger.</summary>
        public Shared.Logging.ILog Log { get; private set; }

        /// <summary>IoC constructor.</summary>
        public CloudStorageProviders(
            IBlobStorageProvider blobStorage,
            IQueueStorageProvider queueStorage,
            ITableStorageProvider tableStorage,
            IRuntimeFinalizer runtimeFinalizer,
            Shared.Logging.ILog log)
        {
            BlobStorage = blobStorage;
            QueueStorage = queueStorage;
            TableStorage = tableStorage;
            RuntimeFinalizer = runtimeFinalizer;
            Log = log;
        }

        /// <summary>Copy constructor.</summary>
        protected CloudStorageProviders(
            CloudStorageProviders copyFrom)
        {
            BlobStorage = copyFrom.BlobStorage;
            QueueStorage = copyFrom.QueueStorage;
            TableStorage = copyFrom.TableStorage;
            RuntimeFinalizer = copyFrom.RuntimeFinalizer;
            Log = copyFrom.Log;
        }
    }
}
