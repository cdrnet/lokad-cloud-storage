#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Storage
{
    /// <summary>Storage providers and runtime providers.</summary>
    public class CloudStorageProviders
    {
        /// <summary>Blob Storage Abstraction.</summary>
        public IBlobStorageProvider BlobStorage { get; private set; }

        /// <summary>Queue Storage Abstraction.</summary>
        public IQueueStorageProvider QueueStorage { get; private set; }

        /// <summary>Table Storage Abstraction.</summary>
        public ITableStorageProvider TableStorage { get; private set; }

        /// <summary>Format-Neutral Blob Storage Abstraction.</summary>
        public IBlobStorageProvider NeutralBlobStorage { get; private set; }

        /// <summary>Format-Neutral Queue Storage Abstraction.</summary>
        public IQueueStorageProvider NeutralQueueStorage { get; private set; }

        /// <summary>Format-Neutral Table Storage Abstraction.</summary>
        public ITableStorageProvider NeutralTableStorage { get; private set; }

        /// <summary>Abstracts the finalizer (used for fast resource release
        /// in case of runtime shutdown).</summary>
        public IRuntimeFinalizer RuntimeFinalizer { get; private set; }

        /// <summary>IoC constructor.</summary>
        public CloudStorageProviders(
            IBlobStorageProvider blobStorage,
            IQueueStorageProvider queueStorage,
            ITableStorageProvider tableStorage,
            IBlobStorageProvider neutralBlobStorage,
            IQueueStorageProvider neutralQueueStorage,
            ITableStorageProvider neutralTableStorage,
            IRuntimeFinalizer runtimeFinalizer = null)
        {
            BlobStorage = blobStorage;
            QueueStorage = queueStorage;
            TableStorage = tableStorage;
            NeutralBlobStorage = neutralBlobStorage;
            NeutralQueueStorage = neutralQueueStorage;
            NeutralTableStorage = neutralTableStorage;
            RuntimeFinalizer = runtimeFinalizer;
        }

        /// <summary>Copy constructor.</summary>
        protected CloudStorageProviders(
            CloudStorageProviders copyFrom)
        {
            BlobStorage = copyFrom.BlobStorage;
            QueueStorage = copyFrom.QueueStorage;
            TableStorage = copyFrom.TableStorage;
            NeutralBlobStorage = copyFrom.NeutralBlobStorage;
            NeutralQueueStorage = copyFrom.NeutralQueueStorage;
            NeutralTableStorage = copyFrom.NeutralTableStorage;
            RuntimeFinalizer = copyFrom.RuntimeFinalizer;
        }
    }
}
