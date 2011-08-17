#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using Lokad.Cloud.Storage.Azure;
using NUnit.Framework;
using System.Text;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Storage.Test.Storage
{
    [TestFixture]
    public class QueueStorageProviderTests
    {
        private const string BaseQueueName = "tests-queuestorageprovider-";
        private string QueueName;

        private static readonly Random _rand = new Random();

        private readonly IQueueStorageProvider _queueStorage;
        private readonly IBlobStorageProvider _blobStorage;

        public QueueStorageProviderTests()
        {
            var storage = CloudStorage.ForDevelopmentStorage().BuildStorageProviders();
            _queueStorage = storage.QueueStorage;
            _blobStorage = storage.BlobStorage;
        }

        protected QueueStorageProviderTests(IQueueStorageProvider queueStorageProvider, IBlobStorageProvider blobStorageProvider)
        {
            _queueStorage = queueStorageProvider;
            _blobStorage = blobStorageProvider;
        }

        [SetUp]
        public void Setup()
        {
            QueueName = BaseQueueName + Guid.NewGuid().ToString("N");
        }

        [TearDown]
        public void TearDown()
        {
            _queueStorage.DeleteQueue(QueueName);
        }

        [Test]
        public void PutGetDelete()
        {
            var message = new MyMessage();

            _queueStorage.DeleteQueue(QueueName); // deleting queue on purpose 
            // (it's slow but necessary to really validate the retry policy)

            _queueStorage.Put(QueueName, message);
            var retrieved = _queueStorage.Get<MyMessage>(QueueName, 1).First();

            Assert.AreEqual(message.MyGuid, retrieved.MyGuid, "#A01");

            _queueStorage.Delete(retrieved);
        }

        [Test]
        public virtual void PutGetDeleteOverflowing()
        {
            // 20k chosen so that it doesn't fit into the queue.
            var message = new MyMessage { MyBuffer = new byte[20000] };

            // fill buffer with random content
            _rand.NextBytes(message.MyBuffer);

            _queueStorage.Clear(QueueName);

            _queueStorage.Put(QueueName, message);
            var retrieved = _queueStorage.Get<MyMessage>(QueueName, 1).First();

            Assert.AreEqual(message.MyGuid, retrieved.MyGuid, "#A01");
            CollectionAssert.AreEquivalent(message.MyBuffer, retrieved.MyBuffer, "#A02");

            for (int i = 0; i < message.MyBuffer.Length; i++)
            {
                Assert.AreEqual(message.MyBuffer[i], retrieved.MyBuffer[i], "#A02-" + i);
            }

            _queueStorage.Delete(retrieved);
        }

        [Test]
        public void PutGetDeleteIdenticalStructOrNative()
        {
            var testStruct = new MyStruct
                {
                    IntegerValue = 12,
                    StringValue = "hello"
                };

            for (int i = 0; i < 10; i++)
            {
                _queueStorage.Put(QueueName, testStruct);
            }

            var outStruct1 = _queueStorage.Get<MyStruct>(QueueName, 1).First();
            var outStruct2 = _queueStorage.Get<MyStruct>(QueueName, 1).First();
            Assert.IsTrue(_queueStorage.Delete(outStruct1), "1st Delete failed");
            Assert.IsTrue(_queueStorage.Delete(outStruct2), "2nd Delete failed");
            Assert.IsFalse(_queueStorage.Delete(outStruct2), "3nd Delete succeeded");

            var outAllStructs = _queueStorage.Get<MyStruct>(QueueName, 20);
            Assert.AreEqual(8, outAllStructs.Count(), "Wrong queue item count");
            foreach (var str in outAllStructs)
            {
                Assert.AreEqual(testStruct.IntegerValue, str.IntegerValue, "Wrong integer value");
                Assert.AreEqual(testStruct.StringValue, str.StringValue, "Wrong string value");
                Assert.IsTrue(_queueStorage.Delete(str), "Delete failed");
            }

            const double testDouble = 3.6D;

            for (int i = 0; i < 10; i++)
            {
                _queueStorage.Put(QueueName, testDouble);
            }

            var outDouble1 = _queueStorage.Get<double>(QueueName, 1).First();
            var outDouble2 = _queueStorage.Get<double>(QueueName, 1).First();
            var outDouble3 = _queueStorage.Get<double>(QueueName, 1).First();
            Assert.IsTrue(_queueStorage.Delete(outDouble1), "1st Delete failed");
            Assert.IsTrue(_queueStorage.Delete(outDouble2), "2nd Delete failed");
            Assert.IsTrue(_queueStorage.Delete(outDouble3), "3nd Delete failed");
            Assert.IsFalse(_queueStorage.Delete(outDouble2), "3nd Delete succeeded");

            var outAllDoubles = _queueStorage.Get<double>(QueueName, 20);
            Assert.AreEqual(7, outAllDoubles.Count(), "Wrong queue item count");
            foreach (var dbl in outAllDoubles)
            {
                Assert.AreEqual(testDouble, dbl, "Wrong double value");
                Assert.IsTrue(_queueStorage.Delete(dbl), "Delete failed");
            }

            const string testString = "hi there!";

            for (int i = 0; i < 10; i++)
            {
                _queueStorage.Put(QueueName, testString);
            }

            var outString1 = _queueStorage.Get<string>(QueueName, 1).First();
            var outString2 = _queueStorage.Get<string>(QueueName, 1).First();
            Assert.IsTrue(_queueStorage.Delete(outString1), "1st Delete failed");
            Assert.IsTrue(_queueStorage.Delete(outString2), "2nd Delete failed");
            Assert.IsFalse(_queueStorage.Delete(outString2), "3nd Delete succeeded");

            var outAllStrings = _queueStorage.Get<string>(QueueName, 20);
            Assert.AreEqual(8, outAllStrings.Count(), "Wrong queue item count");
            foreach (var str in outAllStrings)
            {
                Assert.AreEqual(testString, str, "Wrong string value");
                Assert.IsTrue(_queueStorage.Delete(str), "Delete failed");
            }

            var testClass = new StringBuilder("text");

            for (int i = 0; i < 10; i++)
            {
                _queueStorage.Put(QueueName, testClass);
            }

            var outClass1 = _queueStorage.Get<StringBuilder>(QueueName, 1).First();
            var outClass2 = _queueStorage.Get<StringBuilder>(QueueName, 1).First();
            Assert.IsTrue(_queueStorage.Delete(outClass1), "1st Delete failed");
            Assert.IsTrue(_queueStorage.Delete(outClass2), "2nd Delete failed");
            Assert.IsFalse(_queueStorage.Delete(outClass2), "3nd Delete succeeded");

            var outAllClasses = _queueStorage.Get<StringBuilder>(QueueName, 20);
            Assert.AreEqual(8, outAllClasses.Count(), "Wrong queue item count");
            foreach (var cls in outAllClasses)
            {
                Assert.AreEqual(testClass.ToString(), cls.ToString(), "Wrong deserialized class value");
                Assert.IsTrue(_queueStorage.Delete(cls), "Delete failed");
            }
        }

        // TODO: create same unit test for Clear()

        [Test]
        public virtual void DeleteRemovesOverflowingBlobs()
        {
            var queueName = "test1-" + Guid.NewGuid().ToString("N");

            // CAUTION: we are now compressing serialization output.
            // hence, we can't just pass an empty array, as it would be compressed at near 100%.

            var data = new byte[20000];
            _rand.NextBytes(data);

            _queueStorage.Put(queueName, data);

            // HACK: implicit pattern for listing overflowing messages
            var overflowingCount = _blobStorage.ListBlobNames(
                QueueStorageProvider.OverflowingMessagesContainerName, queueName).Count();

            Assert.AreEqual(1, overflowingCount, "#A00");

            _queueStorage.DeleteQueue(queueName);

            overflowingCount = _blobStorage.ListBlobNames(
                QueueStorageProvider.OverflowingMessagesContainerName, queueName).Count();

            Assert.AreEqual(0, overflowingCount, "#A01");
        }

        [Test]
        public virtual void ClearRemovesOverflowingBlobs()
        {
            var queueName = "test1-" + Guid.NewGuid().ToString("N");

            // CAUTION: we are now compressing serialization output.
            // hence, we can't just pass an empty array, as it would be compressed at near 100%.

            var data = new byte[20000];
            _rand.NextBytes(data);

            _queueStorage.Put(queueName, data);

            // HACK: implicit pattern for listing overflowing messages
            var overflowingCount = _blobStorage.ListBlobNames(
                QueueStorageProvider.OverflowingMessagesContainerName, queueName).Count();

            Assert.AreEqual(1, overflowingCount, "#A00");

            _queueStorage.Clear(queueName);

            overflowingCount = _blobStorage.ListBlobNames(
                QueueStorageProvider.OverflowingMessagesContainerName, queueName).Count();

            Assert.AreEqual(0, overflowingCount, "#A01");

            _queueStorage.DeleteQueue(queueName);
        }

        [Test]
        public void PutGetAbandonDelete()
        {
            var message = new MyMessage();

            _queueStorage.DeleteQueue(QueueName); // deleting queue on purpose 
            // (it's slow but necessary to really validate the retry policy)

            // put
            _queueStorage.Put(QueueName, message);

            // get
            var retrieved = _queueStorage.Get<MyMessage>(QueueName, 1).First();
            Assert.AreEqual(message.MyGuid, retrieved.MyGuid, "#A01");

            // abandon
            var abandoned = _queueStorage.Abandon(retrieved);
            Assert.IsTrue(abandoned, "#A02");

            // abandon II should fail (since not invisible)
            var abandoned2 = _queueStorage.Abandon(retrieved);
            Assert.IsFalse(abandoned2, "#A03");

            // get again
            var retrieved2 = _queueStorage.Get<MyMessage>(QueueName, 1).First();
            Assert.AreEqual(message.MyGuid, retrieved2.MyGuid, "#A04");

            // delete
            var deleted = _queueStorage.Delete(retrieved2);
            Assert.IsTrue(deleted, "#A05");

            // get now should fail
            var retrieved3 = _queueStorage.Get<MyMessage>(QueueName, 1).FirstOrDefault();
            Assert.IsNull(retrieved3, "#A06");

            // abandon does not put it to the queue again
            var abandoned3 = _queueStorage.Abandon(retrieved2);
            Assert.IsFalse(abandoned3, "#A07");

            // get now should still fail
            var retrieved4 = _queueStorage.Get<MyMessage>(QueueName, 1).FirstOrDefault();
            Assert.IsNull(retrieved4, "#A07");
        }

        [Test]
        public void PersistRestore()
        {
            const string storeName = "TestStore";

            var message = new MyMessage();

            // clean up
            _queueStorage.DeleteQueue(QueueName);
            foreach (var skey in _queueStorage.ListPersisted(storeName))
            {
                _queueStorage.DeletePersisted(storeName, skey);
            }

            // put
            _queueStorage.Put(QueueName, message);

            // get
            var retrieved = _queueStorage.Get<MyMessage>(QueueName, 1).First();
            Assert.AreEqual(message.MyGuid, retrieved.MyGuid, "#A01");

            // persist
            _queueStorage.Persist(retrieved, storeName, "manual test");

            // abandon should fail (since not invisible anymore)
            Assert.IsFalse(_queueStorage.Abandon(retrieved), "#A02");

            // list persisted message
            var key = _queueStorage.ListPersisted(storeName).Single();

            // get persisted message
            var persisted = _queueStorage.GetPersisted(storeName, key);
            Assert.IsTrue(persisted.HasValue, "#A03");
            Assert.IsTrue(persisted.Value.DataXml.HasValue, "#A04");
            var xml = persisted.Value.DataXml.Value;
            var property = xml.Elements().Single(x => x.Name.LocalName == "MyGuid");
            Assert.AreEqual(message.MyGuid, new Guid(property.Value), "#A05");

            // restore persisted message
            _queueStorage.RestorePersisted(storeName, key);

            // list no longer contains key
            Assert.IsFalse(_queueStorage.ListPersisted(storeName).Any(), "#A06");

            // get
            var retrieved2 = _queueStorage.Get<MyMessage>(QueueName, 1).First();
            Assert.AreEqual(message.MyGuid, retrieved2.MyGuid, "#A07");

            // delete
            Assert.IsTrue(_queueStorage.Delete(retrieved2), "#A08");
        }

        [Test]
        public virtual void PersistRestoreOverflowing()
        {
            const string storeName = "TestStore";

            // CAUTION: we are now compressing serialization output.
            // hence, we can't just pass an empty array, as it would be compressed at near 100%.

            var data = new byte[20000];
            _rand.NextBytes(data);

            // clean up
            _queueStorage.DeleteQueue(QueueName);
            foreach (var skey in _queueStorage.ListPersisted(storeName))
            {
                _queueStorage.DeletePersisted(storeName, skey);
            }

            // put
            _queueStorage.Put(QueueName, data);

            Assert.AreEqual(1, _blobStorage.ListBlobNames(
                QueueStorageProvider.OverflowingMessagesContainerName, QueueName).Count(), "#A01");

            // get
            var retrieved = _queueStorage.Get<byte[]>(QueueName, 1).First();

            // persist
            _queueStorage.Persist(retrieved, storeName, "manual test");

            Assert.AreEqual(1, _blobStorage.ListBlobNames(
                QueueStorageProvider.OverflowingMessagesContainerName, QueueName).Count(), "#A02");

            // abandon should fail (since not invisible anymore)
            Assert.IsFalse(_queueStorage.Abandon(retrieved), "#A03");

            // list persisted message
            var key = _queueStorage.ListPersisted(storeName).Single();

            // get persisted message
            var persisted = _queueStorage.GetPersisted(storeName, key);
            Assert.IsTrue(persisted.HasValue, "#A04");
            Assert.IsTrue(persisted.Value.DataXml.HasValue, "#A05");

            // delete persisted message
            _queueStorage.DeletePersisted(storeName, key);

            Assert.AreEqual(0, _blobStorage.ListBlobNames(
                QueueStorageProvider.OverflowingMessagesContainerName, QueueName).Count(), "#A06");

            // list no longer contains key
            Assert.IsFalse(_queueStorage.ListPersisted(storeName).Any(), "#A07");
        }

        [Test]
        public virtual void QueueLatency()
        {
            Assert.IsFalse(_queueStorage.GetApproximateLatency(QueueName).HasValue);

            _queueStorage.Put(QueueName, 100);

            var latency = _queueStorage.GetApproximateLatency(QueueName);
            Assert.IsTrue(latency.HasValue);
            Assert.IsTrue(latency.Value >= TimeSpan.Zero && latency.Value < TimeSpan.FromMinutes(10));

            _queueStorage.Delete(100);
        }
    }

    [Serializable]
    public struct MyStruct
    {
        public int IntegerValue;
        public string StringValue;
    }

    [DataContract]
    public class MyMessage
    {
        [DataMember(IsRequired = false)]
        public Guid MyGuid { get; private set; }

        [DataMember]
        public byte[] MyBuffer { get; set; }

        public MyMessage()
        {
            MyGuid = Guid.NewGuid();
        }
    }
}
