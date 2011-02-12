#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Threading;
using Lokad.Cloud.Shared.Test;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.Shared;
using Lokad.Cloud.Storage.Shared.Threading;
using NUnit.Framework;

namespace Lokad.Cloud.Test.Storage
{
    [TestFixture]
    public class BlobCounterTests
    {
        private const string ContainerName = "tests-blobcounter-mycontainer";
        private const string BlobName = "myprefix/myblob";

        [Test]
        public void GetValueIncrement()
        {
            var provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
            provider.CreateContainerIfNotExist(ContainerName);

            var counter = new BlobCounter(provider, ContainerName, BlobName);

            var val = (int)counter.GetValue();

            if (0 != val) counter.Delete();

            counter.Increment(10);
            val = (int) counter.GetValue();
            Assert.AreEqual(10, val, "#A00");

            var val2 = counter.Increment(-5);
            val = (int)counter.GetValue();
            Assert.AreEqual(5, val, "#A01");
            Assert.AreEqual(val, val2, "#A02");

            var flag1 = counter.Delete();
            var flag2 = counter.Delete();

            Assert.IsTrue(flag1, "#A03");
            Assert.IsFalse(flag2, "#A04");
        }

        [Test]
        public void IncrementMultiThread()
        {
            var provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
            provider.CreateContainerIfNotExist(ContainerName);

            //creating thread parameters
            var counter = new BlobCounter(provider, ContainerName, "SomeBlobName");
            counter.Reset(0);

            var random = new Random();
            const int threadsCount = 4;
            var increments = Range.Array(threadsCount).Select(e => Range.Array(5).Select(i => random.Next(20)).ToArray()).ToArray();
            var localSums = increments.SelectInParallel(
                    e =>
                {
                    var c = new BlobCounter(provider, ContainerName, "SomeBlobName");
                    foreach (var increment in e)
                    {
                        c.Increment(increment);
                    }
                    return e.Sum();
                }, threadsCount);

            Assert.AreEqual(increments.Sum(i => i.Sum()), localSums.Sum(), "Broken invariant.");
            Assert.AreEqual(localSums.Sum(), counter.GetValue(), "Values should be equal, BlobCounter supposed to be thread-safe");
        }
    }
}
