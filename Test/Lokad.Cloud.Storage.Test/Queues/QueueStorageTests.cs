#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using NUnit.Framework;
using System.Text;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Storage.Test.Queues
{
    [TestFixture]
    public abstract class QueueStorageTests
    {
        private const string BaseQueueName = "tests-queuestorageprovider-";
        protected string QueueName;

        protected readonly IQueueStorageProvider QueueStorage;
        protected readonly IBlobStorageProvider BlobStorage;

        protected QueueStorageTests(CloudStorageProviders storage)
        {
            QueueStorage = storage.QueueStorage;
            BlobStorage = storage.BlobStorage;
        }

        [SetUp]
        public void Setup()
        {
            QueueName = BaseQueueName + Guid.NewGuid().ToString("N");
        }

        [TearDown]
        public void TearDown()
        {
            QueueStorage.DeleteQueue(QueueName);
        }

        [Test]
        public void PutGetDelete()
        {
            var message = new MyMessage();

            QueueStorage.DeleteQueue(QueueName); // deleting queue on purpose 
            // (it's slow but necessary to really validate the retry policy)

            QueueStorage.Put(QueueName, message);
            var retrieved = QueueStorage.Get<MyMessage>(QueueName, 1).First();

            Assert.AreEqual(message.MyGuid, retrieved.MyGuid, "#A01");

            QueueStorage.Delete(retrieved);
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
                QueueStorage.Put(QueueName, testStruct);
            }

            var outStruct1 = QueueStorage.Get<MyStruct>(QueueName, 1).First();
            var outStruct2 = QueueStorage.Get<MyStruct>(QueueName, 1).First();
            Assert.IsTrue(QueueStorage.Delete(outStruct1), "1st Delete failed");
            Assert.IsTrue(QueueStorage.Delete(outStruct2), "2nd Delete failed");
            Assert.IsFalse(QueueStorage.Delete(outStruct2), "3nd Delete succeeded");

            var outAllStructs = QueueStorage.Get<MyStruct>(QueueName, 20);
            Assert.AreEqual(8, outAllStructs.Count(), "Wrong queue item count");
            foreach (var str in outAllStructs)
            {
                Assert.AreEqual(testStruct.IntegerValue, str.IntegerValue, "Wrong integer value");
                Assert.AreEqual(testStruct.StringValue, str.StringValue, "Wrong string value");
                Assert.IsTrue(QueueStorage.Delete(str), "Delete failed");
            }

            const double testDouble = 3.6D;

            for (int i = 0; i < 10; i++)
            {
                QueueStorage.Put(QueueName, testDouble);
            }

            var outDouble1 = QueueStorage.Get<double>(QueueName, 1).First();
            var outDouble2 = QueueStorage.Get<double>(QueueName, 1).First();
            var outDouble3 = QueueStorage.Get<double>(QueueName, 1).First();
            Assert.IsTrue(QueueStorage.Delete(outDouble1), "1st Delete failed");
            Assert.IsTrue(QueueStorage.Delete(outDouble2), "2nd Delete failed");
            Assert.IsTrue(QueueStorage.Delete(outDouble3), "3nd Delete failed");
            Assert.IsFalse(QueueStorage.Delete(outDouble2), "3nd Delete succeeded");

            var outAllDoubles = QueueStorage.Get<double>(QueueName, 20);
            Assert.AreEqual(7, outAllDoubles.Count(), "Wrong queue item count");
            foreach (var dbl in outAllDoubles)
            {
                Assert.AreEqual(testDouble, dbl, "Wrong double value");
                Assert.IsTrue(QueueStorage.Delete(dbl), "Delete failed");
            }

            const string testString = "hi there!";

            for (int i = 0; i < 10; i++)
            {
                QueueStorage.Put(QueueName, testString);
            }

            var outString1 = QueueStorage.Get<string>(QueueName, 1).First();
            var outString2 = QueueStorage.Get<string>(QueueName, 1).First();
            Assert.IsTrue(QueueStorage.Delete(outString1), "1st Delete failed");
            Assert.IsTrue(QueueStorage.Delete(outString2), "2nd Delete failed");
            Assert.IsFalse(QueueStorage.Delete(outString2), "3nd Delete succeeded");

            var outAllStrings = QueueStorage.Get<string>(QueueName, 20);
            Assert.AreEqual(8, outAllStrings.Count(), "Wrong queue item count");
            foreach (var str in outAllStrings)
            {
                Assert.AreEqual(testString, str, "Wrong string value");
                Assert.IsTrue(QueueStorage.Delete(str), "Delete failed");
            }

            var testClass = new StringBuilder("text");

            for (int i = 0; i < 10; i++)
            {
                QueueStorage.Put(QueueName, testClass);
            }

            var outClass1 = QueueStorage.Get<StringBuilder>(QueueName, 1).First();
            var outClass2 = QueueStorage.Get<StringBuilder>(QueueName, 1).First();
            Assert.IsTrue(QueueStorage.Delete(outClass1), "1st Delete failed");
            Assert.IsTrue(QueueStorage.Delete(outClass2), "2nd Delete failed");
            Assert.IsFalse(QueueStorage.Delete(outClass2), "3nd Delete succeeded");

            var outAllClasses = QueueStorage.Get<StringBuilder>(QueueName, 20);
            Assert.AreEqual(8, outAllClasses.Count(), "Wrong queue item count");
            foreach (var cls in outAllClasses)
            {
                Assert.AreEqual(testClass.ToString(), cls.ToString(), "Wrong deserialized class value");
                Assert.IsTrue(QueueStorage.Delete(cls), "Delete failed");
            }
        }

        // TODO: create same unit test for Clear()

        [Test]
        public void PutGetAbandonDelete()
        {
            var message = new MyMessage();

            QueueStorage.DeleteQueue(QueueName); // deleting queue on purpose 
            // (it's slow but necessary to really validate the retry policy)

            // put
            QueueStorage.Put(QueueName, message);

            // get
            var retrieved = QueueStorage.Get<MyMessage>(QueueName, 1).First();
            Assert.AreEqual(message.MyGuid, retrieved.MyGuid, "#A01");

            // abandon
            var abandoned = QueueStorage.Abandon(retrieved);
            Assert.IsTrue(abandoned, "#A02");

            // abandon II should fail (since not invisible)
            var abandoned2 = QueueStorage.Abandon(retrieved);
            Assert.IsFalse(abandoned2, "#A03");

            // get again
            var retrieved2 = QueueStorage.Get<MyMessage>(QueueName, 1).First();
            Assert.AreEqual(message.MyGuid, retrieved2.MyGuid, "#A04");

            // delete
            var deleted = QueueStorage.Delete(retrieved2);
            Assert.IsTrue(deleted, "#A05");

            // get now should fail
            var retrieved3 = QueueStorage.Get<MyMessage>(QueueName, 1).FirstOrDefault();
            Assert.IsNull(retrieved3, "#A06");

            // abandon does not put it to the queue again
            var abandoned3 = QueueStorage.Abandon(retrieved2);
            Assert.IsFalse(abandoned3, "#A07");

            // get now should still fail
            var retrieved4 = QueueStorage.Get<MyMessage>(QueueName, 1).FirstOrDefault();
            Assert.IsNull(retrieved4, "#A07");
        }

        [Test]
        public void PersistRestore()
        {
            const string storeName = "TestStore";

            var message = new MyMessage();

            // clean up
            QueueStorage.DeleteQueue(QueueName);
            foreach (var skey in QueueStorage.ListPersisted(storeName))
            {
                QueueStorage.DeletePersisted(storeName, skey);
            }

            // put
            QueueStorage.Put(QueueName, message);

            // get
            var retrieved = QueueStorage.Get<MyMessage>(QueueName, 1).First();
            Assert.AreEqual(message.MyGuid, retrieved.MyGuid, "#A01");

            // persist
            QueueStorage.Persist(retrieved, storeName, "manual test");

            // abandon should fail (since not invisible anymore)
            Assert.IsFalse(QueueStorage.Abandon(retrieved), "#A02");

            // list persisted message
            var key = QueueStorage.ListPersisted(storeName).Single();

            // get persisted message
            var persisted = QueueStorage.GetPersisted(storeName, key);
            Assert.IsTrue(persisted.HasValue, "#A03");
            Assert.IsTrue(persisted.Value.DataXml.HasValue, "#A04");
            var xml = persisted.Value.DataXml.Value;
            var property = xml.Elements().Single(x => x.Name.LocalName == "MyGuid");
            Assert.AreEqual(message.MyGuid, new Guid(property.Value), "#A05");

            // restore persisted message
            QueueStorage.RestorePersisted(storeName, key);

            // list no longer contains key
            Assert.IsFalse(QueueStorage.ListPersisted(storeName).Any(), "#A06");

            // get
            var retrieved2 = QueueStorage.Get<MyMessage>(QueueName, 1).First();
            Assert.AreEqual(message.MyGuid, retrieved2.MyGuid, "#A07");

            // delete
            Assert.IsTrue(QueueStorage.Delete(retrieved2), "#A08");
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
