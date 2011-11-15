#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using Lokad.Cloud.Storage.Azure;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test.Queues
{
    [TestFixture]
    [Category("DevelopmentStorage")]
    public class DevQueueStorageTests : QueueStorageTests
    {
        private static readonly Random _rand = new Random();

        public DevQueueStorageTests()
            : base(CloudStorage.ForDevelopmentStorage().BuildStorageProviders())
        {
        }

        [Test]
        public void PutGetDeleteOverflowing()
        {
            // 20k chosen so that it doesn't fit into the queue.
            var message = new MyMessage { MyBuffer = new byte[80000] };

            // fill buffer with random content
            _rand.NextBytes(message.MyBuffer);

            QueueStorage.Clear(QueueName);

            QueueStorage.Put(QueueName, message);
            var retrieved = QueueStorage.Get<MyMessage>(QueueName, 1).First();

            Assert.AreEqual(message.MyGuid, retrieved.MyGuid, "#A01");
            CollectionAssert.AreEquivalent(message.MyBuffer, retrieved.MyBuffer, "#A02");

            for (int i = 0; i < message.MyBuffer.Length; i++)
            {
                Assert.AreEqual(message.MyBuffer[i], retrieved.MyBuffer[i], "#A02-" + i);
            }

            QueueStorage.Delete(retrieved);
        }

        [Test]
        public void DeleteRemovesOverflowingBlobs()
        {
            var queueName = "test1-" + Guid.NewGuid().ToString("N");

            // CAUTION: we are now compressing serialization output.
            // hence, we can't just pass an empty array, as it would be compressed at near 100%.

            var data = new byte[80000];
            _rand.NextBytes(data);

            QueueStorage.Put(queueName, data);

            // HACK: implicit pattern for listing overflowing messages
            var overflowingCount =
                BlobStorage.ListBlobNames(QueueStorageProvider.OverflowingMessagesContainerName, queueName).Count();

            Assert.AreEqual(1, overflowingCount, "#A00");

            QueueStorage.DeleteQueue(queueName);

            overflowingCount =
                BlobStorage.ListBlobNames(QueueStorageProvider.OverflowingMessagesContainerName, queueName).Count();

            Assert.AreEqual(0, overflowingCount, "#A01");
        }

        [Test]
        public void ClearRemovesOverflowingBlobs()
        {
            var queueName = "test1-" + Guid.NewGuid().ToString("N");

            // CAUTION: we are now compressing serialization output.
            // hence, we can't just pass an empty array, as it would be compressed at near 100%.

            var data = new byte[80000];
            _rand.NextBytes(data);

            QueueStorage.Put(queueName, data);

            // HACK: implicit pattern for listing overflowing messages
            var overflowingCount =
                BlobStorage.ListBlobNames(QueueStorageProvider.OverflowingMessagesContainerName, queueName).Count();

            Assert.AreEqual(1, overflowingCount, "#A00");

            QueueStorage.Clear(queueName);

            overflowingCount =
                BlobStorage.ListBlobNames(QueueStorageProvider.OverflowingMessagesContainerName, queueName).Count();

            Assert.AreEqual(0, overflowingCount, "#A01");

            QueueStorage.DeleteQueue(queueName);
        }

        [Test]
        public void PersistRestoreOverflowing()
        {
            const string storeName = "TestStore";

            // CAUTION: we are now compressing serialization output.
            // hence, we can't just pass an empty array, as it would be compressed at near 100%.

            var data = new byte[80000];
            _rand.NextBytes(data);

            // clean up
            QueueStorage.DeleteQueue(QueueName);
            foreach (var skey in QueueStorage.ListPersisted(storeName))
            {
                QueueStorage.DeletePersisted(storeName, skey);
            }

            // put
            QueueStorage.Put(QueueName, data);

            Assert.AreEqual(
                1,
                BlobStorage.ListBlobNames(QueueStorageProvider.OverflowingMessagesContainerName, QueueName).Count(),
                "#A01");

            // get
            var retrieved = QueueStorage.Get<byte[]>(QueueName, 1).First();

            // persist
            QueueStorage.Persist(retrieved, storeName, "manual test");

            Assert.AreEqual(
                1,
                BlobStorage.ListBlobNames(QueueStorageProvider.OverflowingMessagesContainerName, QueueName).Count(),
                "#A02");

            // abandon should fail (since not invisible anymore)
            Assert.IsFalse(QueueStorage.Abandon(retrieved), "#A03");

            // list persisted message
            var key = QueueStorage.ListPersisted(storeName).Single();

            // get persisted message
            var persisted = QueueStorage.GetPersisted(storeName, key);
            Assert.IsTrue(persisted.HasValue, "#A04");
            Assert.IsTrue(persisted.Value.DataXml.HasValue, "#A05");

            // delete persisted message
            QueueStorage.DeletePersisted(storeName, key);

            Assert.AreEqual(
                0,
                BlobStorage.ListBlobNames(QueueStorageProvider.OverflowingMessagesContainerName, QueueName).Count(),
                "#A06");

            // list no longer contains key
            Assert.IsFalse(QueueStorage.ListPersisted(storeName).Any(), "#A07");
        }

        [Test]
        public void QueueLatency()
        {
            Assert.IsFalse(QueueStorage.GetApproximateLatency(QueueName).HasValue);

            QueueStorage.Put(QueueName, 100);

            var latency = QueueStorage.GetApproximateLatency(QueueName);
            Assert.IsTrue(latency.HasValue);
            Assert.IsTrue(latency.Value >= TimeSpan.Zero && latency.Value < TimeSpan.FromMinutes(10));

            QueueStorage.Delete(100);
        }
    }
}
