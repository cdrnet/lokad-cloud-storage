#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test.Queues
{
    [TestFixture]
    [Category("InMemoryStorage")]
    public class MemoryQueueStorageTests : QueueStorageTests
    {
        private const string FirstQueueName = "firstQueueName";
        private const string SecondQueueName = "secondQueueName";

        public MemoryQueueStorageTests()
            : base(CloudStorage.ForInMemoryStorage().BuildStorageProviders())
        {
        }

        [TearDown]
        public void TearDown()
        {
            QueueStorage.DeleteQueue(FirstQueueName);
            QueueStorage.DeleteQueue(SecondQueueName);
        }

        [Test]
        public void GetOnMissingQueueDoesNotFail()
        {
            QueueStorage.Get<int>("nosuchqueue", 1);
        }

        [Test]
        public void ItemsGetPutInMonoThread()
        {
            var fakeMessages = Enumerable.Range(0, 3).Select(i => new FakeMessage(i)).ToArray();
            
            QueueStorage.PutRange(FirstQueueName, fakeMessages.Take(2));
            QueueStorage.PutRange(SecondQueueName, fakeMessages.Skip(2).ToArray());

            Assert.AreEqual(
                2,
                QueueStorage.GetApproximateCount(FirstQueueName),
                "#A04 First queue has not the right number of elements.");
            Assert.AreEqual(
                1,
                QueueStorage.GetApproximateCount(SecondQueueName),
                "#A05 Second queue has not the right number of elements.");
        }

        [Test]
        public void ItemsReturnedInMonoThread()
        {
            var fakeMessages = Enumerable.Range(0, 10).Select(i => new FakeMessage(i)).ToArray();

            QueueStorage.PutRange(FirstQueueName, fakeMessages.Take(6));
            var allFirstItems = QueueStorage.Get<FakeMessage>(FirstQueueName, 6);
            QueueStorage.Clear(FirstQueueName);

            QueueStorage.PutRange(FirstQueueName, fakeMessages.Take(6));
            var partOfFirstItems = QueueStorage.Get<FakeMessage>(FirstQueueName, 2);
            Assert.AreEqual(4, QueueStorage.GetApproximateCount(FirstQueueName), "#A06");
            QueueStorage.Clear(FirstQueueName);

            QueueStorage.PutRange(FirstQueueName, fakeMessages.Take(6));
            var allFirstItemsAndMore = QueueStorage.Get<FakeMessage>(FirstQueueName, 8);
            QueueStorage.Clear(FirstQueueName);

            Assert.AreEqual(6, allFirstItems.Count(), "#A07");
            Assert.AreEqual(2, partOfFirstItems.Count(), "#A08");
            Assert.AreEqual(6, allFirstItemsAndMore.Count(), "#A09");
        }

        [Test]
        public void ListInMonoThread()
        {
            var fakeMessages = Enumerable.Range(0, 10).Select(i => new FakeMessage(i)).ToArray();

            QueueStorage.PutRange(FirstQueueName, fakeMessages.Take(6));
            var queuesName = QueueStorage.List("");

            Assert.AreEqual(1, queuesName.Count(), "#A010");
        }

        [Serializable]
        private class FakeMessage
        {
            private double Value { get; set; }

            public FakeMessage(double value)
            {
                Value = value;
            }
        }
    }
}
