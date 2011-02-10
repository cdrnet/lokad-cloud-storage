#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Threading;
using Lokad.Cloud.Storage.InMemory;
using NUnit.Framework;

namespace Lokad.Cloud.Test.Storage.InMemory
{
    [TestFixture]
    public class MemoryBlobStorageProviderTests : BlobStorageProviderTests
    {
        public MemoryBlobStorageProviderTests()
            : base(new MemoryBlobStorageProvider())
        {
        }

        [Obsolete]
        public override void UpdateIfNotModifiedWithStress()
        {
            // Test invalid with memory blob storage because of its atomic implementation.
        }

        [Test]
        public void BlobsGetCreatedMonoThread()
        {
            const string containerName1 = "container-1";
            const string containerName2 = "container-2";
            const string containerName3 = "container-3";
            const string blobPrefix = "mockBlobPrefix";
            const string secondBlobPrefix = "sndBlobPrefix";

            var storage = new MemoryBlobStorageProvider();

            storage.CreateContainerIfNotExist(containerName1);
            storage.CreateContainerIfNotExist(containerName2);
            storage.CreateContainerIfNotExist(containerName3);

            storage.PutBlob(containerName1, blobPrefix + "/" + "blob1", new DateTime(2009,08,27));
            storage.PutBlob(containerName1, blobPrefix + "/" + "blob2", new DateTime(2009, 08, 28));
            storage.PutBlob(containerName1, blobPrefix + "/" + "blob3", new DateTime(2009, 08, 29));
            storage.PutBlob(containerName2, blobPrefix + "/" + "blob2", new DateTime(1984, 07, 06));
            storage.PutBlob(containerName1, secondBlobPrefix + "/" + "blob1", new DateTime(2009, 08, 30));

            Assert.AreEqual(3, storage.ListBlobNames(containerName1, blobPrefix).Count(),
                "first container with first prefix does not hold 3 blobs");

            Assert.AreEqual(1, storage.ListBlobNames(containerName2, blobPrefix).Count(),
                "second container with first prefix does not hold 1 blobs");

            Assert.AreEqual(0, storage.ListBlobNames(containerName3, blobPrefix).Count(),
                "third container with first prefix does not hold 0 blob");

            Assert.AreEqual(1, storage.ListBlobNames(containerName1, secondBlobPrefix).Count(),
                "first container with second prefix does not hold 1 blobs");
        }

        [Test]
        public void BlobsGetCreatedMultiThread()
        {
            const string containerNamePrefix = "container-";
            
            const string blobPrefix = "mockBlobPrefix";

            var storage = new MemoryBlobStorageProvider();
            storage.CreateContainerIfNotExist(containerNamePrefix + 1);
            storage.CreateContainerIfNotExist(containerNamePrefix + 2);

            var threads = Enumerable.Range(0, 32)
                                    .Select(i=> 
                                        new Thread(AddValueToContainer))
                                    .ToArray();
            
            var threadParameters = Enumerable.Range(0, 32).Select(i => 
                i<=15
                ? new ThreadParameters("threadId" + i, "container-1", storage)
                : new ThreadParameters("threadId" + i, "container-2", storage)).ToArray();

            foreach (var i in Enumerable.Range(0, 32))
            {
                threads[i].Start(threadParameters[i]);
            }
            
            Thread.Sleep(2000);

            Assert.AreEqual(1600, storage.ListBlobNames("container-1", blobPrefix).Count(),
                "first container with corresponding prefix does not hold 3 blobs");

            Assert.AreEqual(1600, storage.ListBlobNames("container-2", blobPrefix).Count(),
                "second container with corresponding prefix does not hold 1 blobs");

        }

        private static void AddValueToContainer(object parameters)
        {
            if (parameters is ThreadParameters)
            {
                var castedParameters = (ThreadParameters)parameters;
                var random = new Random();
                for (int i = 0; i < 100; i++)
                {
                    castedParameters.BlobStorage.PutBlob(castedParameters.ContainerName, 
                        "mockBlobPrefix" + castedParameters.ThreadId + "/blob" + i, random.NextDouble());
                }
            }
        }

        class ThreadParameters
        {
            public MemoryBlobStorageProvider BlobStorage { get; set; }
            public string ThreadId { get; set; }
            public string ContainerName { get; set; }

            public ThreadParameters(string threadId, string containerName, MemoryBlobStorageProvider blobStorage)
            {
                BlobStorage = blobStorage;
                ThreadId = threadId;
                ContainerName = containerName;
            }
        }
    }
}
