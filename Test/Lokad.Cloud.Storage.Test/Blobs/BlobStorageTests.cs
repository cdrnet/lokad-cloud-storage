#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Storage.Test.Shared;
using NUnit.Framework;

// TODO: refactor tests so that containers do not have to be created each time.

namespace Lokad.Cloud.Storage.Test.Blobs
{
    [TestFixture]
    public abstract class BlobStorageTests
    {
        protected const string ContainerName = "tests-blobstorageprovider-mycontainer";
        protected const string BlobName = "myprefix/myblob";

        protected readonly IBlobStorageProvider BlobStorage;

        protected BlobStorageTests(IBlobStorageProvider storage)
        {
            BlobStorage = storage;
        }

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            BlobStorage.CreateContainerIfNotExist(ContainerName);
            BlobStorage.DeleteBlobIfExist(ContainerName, BlobName);
        }

        [Test]
        public void CanListContainers()
        {
            Assert.IsTrue(BlobStorage.ListContainers().Contains(ContainerName));
            Assert.IsTrue(BlobStorage.ListContainers(ContainerName.Substring(0, 5)).Contains(ContainerName));
            Assert.IsFalse(BlobStorage.ListContainers("another-prefix").Contains(ContainerName));
        }

        [Test]
        public void GetAndDelete()
        {
            BlobStorage.DeleteBlobIfExist(ContainerName, BlobName);
            Assert.IsFalse(BlobStorage.GetBlob<int>(ContainerName, BlobName).HasValue, "#A00");
        }

        [Test]
        public void GetAndDeleteAsync()
        {
            BlobStorage.DeleteBlobIfExist(ContainerName, BlobName);
            Assert.IsNull(BlobStorage.GetBlobAsync<int>(ContainerName, BlobName).Result, "#A00");
        }

        [Test]
        public void BlobHasEtag()
        {
            BlobStorage.PutBlob(ContainerName, BlobName, 1);
            Assert.IsNotNull(BlobStorage.GetBlobEtag(ContainerName, BlobName), "#A00");
        }

        [Test]
        public void BlobHasEtagAsync()
        {
            BlobStorage.PutBlob(ContainerName, BlobName, 1);
            Assert.IsNotNull(BlobStorage.GetBlobEtagAsync(ContainerName, BlobName).Result, "#A00");
        }

        [Test]
        public void MissingBlobHasNoEtag()
        {
            BlobStorage.DeleteBlobIfExist(ContainerName, BlobName);
            Assert.IsNull(BlobStorage.GetBlobEtag(ContainerName, BlobName), "#A00");
        }

        [Test]
        public void MissingBlobHasNoEtagAsync()
        {
            BlobStorage.DeleteBlobIfExist(ContainerName, BlobName);
            Assert.IsNull(BlobStorage.GetBlobEtagAsync(ContainerName, BlobName).Result, "#A00");
        }

        [Test]
        public void PutBlobEnforceNoOverwrite()
        {
            BlobStorage.PutBlob(ContainerName, BlobName, 1);

            string etag;
            var isSaved = BlobStorage.PutBlob(ContainerName, BlobName, 6, false, out etag);
            Assert.IsFalse(isSaved, "#A00");
            Assert.IsNull(etag, "#A01");

            Assert.IsTrue(BlobStorage.GetBlob<int>(ContainerName, BlobName).HasValue, "#A02");
            Assert.AreEqual(1, BlobStorage.GetBlob<int>(ContainerName, BlobName).Value, "#A03");
        }

        [Test]
        public void PutBlobEnforceNoOverwriteAsync()
        {
            BlobStorage.PutBlob(ContainerName, BlobName, 1, true);
            Assert.IsNull(BlobStorage.PutBlobAsync(ContainerName, BlobName, 6, false).Result, "#A01");
            Assert.AreEqual(1, BlobStorage.GetBlobAsync<int>(ContainerName, BlobName).Result.Blob, "#A02");
        }

        [Test]
        public void PutBlobEnforceOverwrite()
        {
            BlobStorage.PutBlob(ContainerName, BlobName, 1);

            string etag;
            var isSaved = BlobStorage.PutBlob(ContainerName, BlobName, 6, true, out etag);
            Assert.IsTrue(isSaved, "#A00");
            Assert.IsNotNull(etag, "#A01");

            var maybe = BlobStorage.GetBlob<int>(ContainerName, BlobName);
            Assert.IsTrue(maybe.HasValue, "#A02");
            Assert.AreEqual(6, maybe.Value, "#A03");
        }

        [Test]
        public void PutBlobEnforceOverwriteAsync()
        {
            BlobStorage.PutBlob(ContainerName, BlobName, 1);
            Assert.IsNotNull(BlobStorage.PutBlobAsync(ContainerName, BlobName, 6, true).Result, "#A01");
            Assert.AreEqual(6, BlobStorage.GetBlobAsync<int>(ContainerName, BlobName).Result.Blob, "#A02");
        }

        /// <summary>The purpose of this test is to further check MD5 behavior
        /// below and above the 32MB threshold (plus the below/above 4MB too).</summary>
        //[Test]
        // HACK: [Vermorel 2010-11] Test is super slow, and cannot complete within
        // less than 60min on the build server.
        public void PutBlobWithGrowingSizes()
        {
            var rand = new Random(0);
            foreach (var i in new [] {/*1, 2, 4,*/ 25, 40})
            {
                var buffer = new byte[(i* 1000000)];
                rand.NextBytes(buffer);

                BlobStorage.PutBlob(ContainerName, BlobName, buffer);
                var maybe = BlobStorage.GetBlob<byte[]>(ContainerName, BlobName);

                Assert.IsTrue(maybe.HasValue);

                for(int j = 0; j < buffer.Length; j++)
                {
                    Assert.AreEqual(buffer[j], maybe.Value[j]);
                }
            }
        }

        [Test]
        public void PutBlobEnforceMatchingEtag()
        {
            BlobStorage.PutBlob(ContainerName, BlobName, 1);

            var etag = BlobStorage.GetBlobEtag(ContainerName, BlobName);
            var isUpdated = BlobStorage.PutBlob(ContainerName, BlobName, 2, Guid.NewGuid().ToString());

            Assert.IsTrue(!isUpdated, "#A00 Blob shouldn't be updated if etag is not matching");

            isUpdated = BlobStorage.PutBlob(ContainerName, BlobName, 3, etag);
            Assert.IsTrue(isUpdated, "#A01 Blob should have been updated");
        }

        [Test]
        public virtual void PutBlobEnforceMatchingEtagAsync()
        {
            BlobStorage.PutBlob(ContainerName, BlobName, 1);
            var etag = BlobStorage.GetBlobEtagAsync(ContainerName, BlobName).Result;
            Assert.IsNull(BlobStorage.PutBlobAsync(ContainerName, BlobName, 2, Guid.NewGuid().ToString()).Result, "#A00 Blob shouldn't be updated if etag is not matching");
            Assert.IsNotNull(BlobStorage.PutBlobAsync(ContainerName, BlobName, 3, etag).Result, "#A01 Blob should have been updated");
        }

        [Test]
        public void EtagChangesOnlyWithBlobChange()
        {
            BlobStorage.PutBlob(ContainerName, BlobName, 1);
            var etag = BlobStorage.GetBlobEtag(ContainerName, BlobName);
            var newEtag = BlobStorage.GetBlobEtag(ContainerName, BlobName);
            Assert.AreEqual(etag, newEtag, "#A00");
        }

        [Test]
        public void EtagChangesOnlyWithBlobChangeAsync()
        {
            BlobStorage.PutBlob(ContainerName, BlobName, 1);
            var etag = BlobStorage.GetBlobEtagAsync(ContainerName, BlobName);
            var newEtag = BlobStorage.GetBlobEtagAsync(ContainerName, BlobName);
            Assert.AreEqual(etag.Result, newEtag.Result, "#A00");
        }

        [Test]
        public virtual void EtagChangesWithBlobChange()
        {
            BlobStorage.PutBlob(ContainerName, BlobName, 1);
            var etag = BlobStorage.GetBlobEtag(ContainerName, BlobName);
            BlobStorage.PutBlob(ContainerName, BlobName, 1);
            var newEtag = BlobStorage.GetBlobEtag(ContainerName, BlobName);
            Assert.AreNotEqual(etag, newEtag, "#A00.");
        }

        [Test]
        public void GetBlobIfNotModifiedNoChangeNoRetrieval()
        {
            BlobStorage.PutBlob(ContainerName, BlobName, 1);
            var etag = BlobStorage.GetBlobEtag(ContainerName, BlobName);

            string newEtag;
            var output = BlobStorage.GetBlobIfModified<MyBlob>(ContainerName, BlobName, etag, out newEtag);

            Assert.IsNull(newEtag, "#A00");
            Assert.IsFalse(output.HasValue, "#A01");
        }

        [Test]
        public void GetBlobIfNotModifiedWithTypeMistmatch()
        {
            BlobStorage.PutBlob(ContainerName, BlobName, 1); // pushing Int32

            string newEtag; // pulling MyBlob
            var output = BlobStorage.GetBlobIfModified<MyBlob>(ContainerName, BlobName, "dummy", out newEtag);
            Assert.IsFalse(output.HasValue);
        }

        /// <remarks>
        /// This test does not check the behavior in case of concurrency stress.
        /// </remarks>
        [Test]
        public void UpsertBlockOrSkipNoStress()
        {
            var blobName = "test" + Guid.NewGuid().ToString("N");
            Assert.IsFalse(BlobStorage.GetBlob<int>(ContainerName, blobName).HasValue);

            int inserted = 0, updated = 10;

            // ReSharper disable AccessToModifiedClosure

            // skip insert
            Assert.IsFalse(BlobStorage.UpsertBlobOrSkip(ContainerName, blobName, () => Maybe<int>.Empty, x => ++updated).HasValue);
            Assert.AreEqual(0, inserted);
            Assert.AreEqual(10, updated);
            Assert.IsFalse(BlobStorage.GetBlob<int>(ContainerName, blobName).HasValue);

            // do insert
            Assert.IsTrue(BlobStorage.UpsertBlobOrSkip<int>(ContainerName, blobName, () => ++inserted, x => ++updated).HasValue);
            Assert.AreEqual(1, inserted);
            Assert.AreEqual(10, updated);
            Assert.AreEqual(1, BlobStorage.GetBlob<int>(ContainerName, blobName).Value);

            // skip update
            Assert.IsFalse(BlobStorage.UpsertBlobOrSkip<int>(ContainerName, blobName, () => ++inserted, x => Maybe<int>.Empty).HasValue);
            Assert.AreEqual(1, inserted);
            Assert.AreEqual(10, updated);
            Assert.AreEqual(1, BlobStorage.GetBlob<int>(ContainerName, blobName).Value);

            // do update
            Assert.IsTrue(BlobStorage.UpsertBlobOrSkip<int>(ContainerName, blobName, () => ++inserted, x => ++updated).HasValue);
            Assert.AreEqual(1, inserted);
            Assert.AreEqual(11, updated);
            Assert.AreEqual(11, BlobStorage.GetBlob<int>(ContainerName, blobName).Value);

            // cleanup
            BlobStorage.DeleteBlobIfExist(ContainerName, blobName);

            // ReSharper restore AccessToModifiedClosure
        }

        /// <remarks>
        /// ASYNC: This test does not check the behavior in case of concurrency stress.
        /// </remarks>
        [Test]
        public void UpsertBlockOrSkipNoStressAsync()
        {
            var blobName = "test" + Guid.NewGuid().ToString("N");
            Assert.IsNull(BlobStorage.GetBlobAsync<int>(ContainerName, blobName).Result);

            int inserted = 0, updated = 10;

            // ReSharper disable AccessToModifiedClosure

            // skip insert
            Assert.IsNull(BlobStorage.UpsertBlobOrSkipAsync(ContainerName, blobName, () => Maybe<int>.Empty, x => Interlocked.Increment(ref updated)).Result);
            Assert.AreEqual(0, inserted);
            Assert.AreEqual(10, updated);
            Assert.IsNull(BlobStorage.GetBlobAsync<int>(ContainerName, blobName).Result);

            // do insert
            Assert.IsNotNull(BlobStorage.UpsertBlobOrSkipAsync<int>(ContainerName, blobName, () => Interlocked.Increment(ref inserted), x => Interlocked.Increment(ref updated)).Result);
            Assert.AreEqual(1, inserted);
            Assert.AreEqual(10, updated);
            Assert.AreEqual(1, BlobStorage.GetBlobAsync<int>(ContainerName, blobName).Result.Blob);

            // skip update
            Assert.IsNull(BlobStorage.UpsertBlobOrSkipAsync<int>(ContainerName, blobName, () => Interlocked.Increment(ref inserted), x => Maybe<int>.Empty).Result);
            Assert.AreEqual(1, inserted);
            Assert.AreEqual(10, updated);
            Assert.AreEqual(1, BlobStorage.GetBlobAsync<int>(ContainerName, blobName).Result.Blob);

            // do update
            Assert.IsNotNull(BlobStorage.UpsertBlobOrSkipAsync<int>(ContainerName, blobName, () => Interlocked.Increment(ref inserted), x => Interlocked.Increment(ref updated)).Result);
            Assert.AreEqual(1, inserted);
            Assert.AreEqual(11, updated);
            Assert.AreEqual(11, BlobStorage.GetBlobAsync<int>(ContainerName, blobName).Result.Blob);

            // cleanup
            BlobStorage.DeleteBlobIfExist(ContainerName, blobName);

            // ReSharper restore AccessToModifiedClosure
        }

        /// <remarks>
        /// Loose check of the behavior under concurrency stress.
        /// </remarks>
        [Test]
        public void UpsertBlockOrSkipWithStress()
        {
            BlobStorage.PutBlob(ContainerName, BlobName, 0);

            var array = new Maybe<int>[8];
            array = array
                .AsParallel()
                .Select(k => BlobStorage.UpsertBlobOrSkip<int>(ContainerName, BlobName, () => 1, i => i + 1))
                .ToArray();

            Assert.IsFalse(array.Any(x => !x.HasValue), "No skips");

            var sorted = array.Select(m => m.Value)
                .OrderBy(i => i)
                .ToArray();

            for (int i = 0; i < array.Length; i++)
            {
                Assert.AreEqual(i + 1, sorted[i], "Concurrency should be resolved, every call should increment by one.");
            }
        }

        /// <remarks>
        /// ASYNC: Loose check of the behavior under concurrency stress.
        /// </remarks>
        [Test]
        public void UpsertBlockOrSkipWithStressAsync()
        {
            BlobStorage.PutBlobAsync(ContainerName, BlobName, 0).Wait();

            var array = new Task<BlobWithETag<int>>[8];
            array = array
                .AsParallel()
                .Select(k => BlobStorage.UpsertBlobOrSkipAsync<int>(ContainerName, BlobName, () => 1, i => i + 1))
                .ToArray();

            Assert.IsFalse(array.Any(x => x.Result == null), "No skips");

            var sorted = array.Select(m => m.Result.Blob)
                .OrderBy(i => i)
                .ToArray();


            for (int i = 0; i < array.Length; i++)
            {
                Assert.AreEqual(i + 1, sorted[i], "Concurrency should be resolved, every call should increment by one.");
            }
        }

        // TODO: CreatePutGetRangeDelete is way too complex as a unit test

        [Test]
        public void CreatePutGetRangeDelete()
        {
            var privateContainerName = "test-" + Guid.NewGuid().ToString("N");

            BlobStorage.CreateContainerIfNotExist(privateContainerName);

            var blobNames = new[]
            {
                BlobName + "-0",
                BlobName + "-1",
                BlobName + "-2",
                BlobName + "-3"
            };

            var inputBlobs = new[]
            {
                new MyBlob(),
                new MyBlob(),
                new MyBlob(),
                new MyBlob()
            };

            for(int i = 0; i < blobNames.Length; i++)
            {
                BlobStorage.PutBlob(privateContainerName, blobNames[i], inputBlobs[i]);
            }

            string[] allEtags;
            var allBlobs = BlobStorage.GetBlobRange<MyBlob>(privateContainerName, blobNames, out allEtags);

            Assert.AreEqual(blobNames.Length, allEtags.Length, "Wrong etags array length");
            Assert.AreEqual(blobNames.Length, allBlobs.Length, "Wrong blobs array length");

            for(int i = 0; i < allBlobs.Length; i++)
            {
                Assert.IsNotNull(allEtags[i], "Etag should have been set");
                Assert.IsTrue(allBlobs[i].HasValue, "Blob should have content");
                Assert.AreEqual(inputBlobs[i].MyGuid, allBlobs[i].Value.MyGuid, "Wrong blob content");
            }

            // Test missing blob
            var wrongBlobNames = new string[blobNames.Length + 1];
            Array.Copy(blobNames, wrongBlobNames, blobNames.Length);
            wrongBlobNames[wrongBlobNames.Length - 1] = "inexistent-blob";

            allBlobs = BlobStorage.GetBlobRange<MyBlob>(privateContainerName, wrongBlobNames, out allEtags);

            Assert.AreEqual(wrongBlobNames.Length, allEtags.Length, "Wrong etags array length");
            Assert.AreEqual(wrongBlobNames.Length, allBlobs.Length, "Wrong blobs array length");

            for(int i = 0; i < allBlobs.Length - 1; i++)
            {
                Assert.IsNotNull(allEtags[i], "Etag should have been set");
                Assert.IsTrue(allBlobs[i].HasValue, "Blob should have content");
                Assert.AreEqual(inputBlobs[i].MyGuid, allBlobs[i].Value.MyGuid, "Wrong blob content");
            }
            Assert.IsNull(allEtags[allEtags.Length - 1], "Etag should be null");
            Assert.IsFalse(allBlobs[allBlobs.Length - 1].HasValue, "Blob should not have a value");

            BlobStorage.DeleteContainerIfExist(privateContainerName);
        }


        [Test]
        public void NullableType_Default()
        {
            var privateContainerName = "test-" + Guid.NewGuid().ToString("N");

            BlobStorage.CreateContainerIfNotExist(privateContainerName);

            int? value1 = 10;
            int? value2 = null;

            BlobStorage.PutBlob(privateContainerName, "test1", value1);
            BlobStorage.PutBlob(privateContainerName, "test2", value1);

            var output1 = BlobStorage.GetBlob<int?>(privateContainerName, "test1");
            var output2 = BlobStorage.GetBlob<int?>(privateContainerName, "test2");

            Assert.AreEqual(value1.Value, output1.Value);
            Assert.IsFalse(value2.HasValue);

            BlobStorage.DeleteContainerIfExist(privateContainerName);
        }

        [Test]
        public void ListBlobNames()
        {
            var prefix = Guid.NewGuid().ToString("N");

            var prefixed = Range.Array(10).Select(i => prefix + Guid.NewGuid().ToString("N")).ToArray();
            var unprefixed = Range.Array(13).Select(i => Guid.NewGuid().ToString("N")).ToArray();

            foreach (var n in prefixed)
            {
                BlobStorage.PutBlob(ContainerName, n, n);
            }

            foreach (var n in unprefixed)
            {
                BlobStorage.PutBlob(ContainerName, n, n);
            }

            var list = BlobStorage.ListBlobNames(ContainerName, prefix).ToArray();

            Assert.AreEqual(prefixed.Length, list.Length, "#A00");

            foreach (var n in list)
            {
                Assert.IsTrue(prefixed.Contains(n), "#A01");
                Assert.IsFalse(unprefixed.Contains(n), "#A02");
            }
        }


        [Test]
        public void ListBlobLocations()
        {
            var prefix = Guid.NewGuid().ToString("N");

            var prefixed = Range.Array(10).Select(i => prefix + Guid.NewGuid().ToString("N")).ToArray();
            var unprefixed = Range.Array(13).Select(i => Guid.NewGuid().ToString("N")).ToArray();

            foreach (var n in prefixed)
            {
                BlobStorage.PutBlob(ContainerName, n, n);
            }

            foreach (var n in unprefixed)
            {
                BlobStorage.PutBlob(ContainerName, n, n);
            }

            var list = BlobStorage.ListBlobLocations(ContainerName, prefix).ToArray();

            Assert.AreEqual(prefixed.Length, list.Length, "#A00");

            foreach (var n in list)
            {
                Assert.AreEqual(ContainerName, n.ContainerName);
                Assert.IsTrue(prefixed.Contains(n.Path), "#A01");
                Assert.IsFalse(unprefixed.Contains(n.Path), "#A02");
            }
        }

        [Test]
        public void ListBlobs()
        {
            var prefix = Guid.NewGuid().ToString("N");

            var prefixed = Range.Array(10).Select(i => prefix + Guid.NewGuid().ToString("N")).ToArray();
            var unprefixed = Range.Array(13).Select(i => Guid.NewGuid().ToString("N")).ToArray();

            foreach (var n in prefixed)
            {
                BlobStorage.PutBlob(ContainerName, n, n);
            }

            foreach (var n in unprefixed)
            {
                BlobStorage.PutBlob(ContainerName, n, n);
            }

            var list = BlobStorage.ListBlobs<string>(ContainerName, prefix).ToArray();

            Assert.AreEqual(prefixed.Length, list.Length, "#A00");

            foreach (var n in list)
            {
                Assert.IsTrue(prefixed.Contains(n), "#A01");
                Assert.IsFalse(unprefixed.Contains(n), "#A02");
            }
        }

        [Test]
        public void GetBlobXml()
        {
            var data = new MyBlob();
            BlobStorage.PutBlob(ContainerName, BlobName, data, true);

            string ignored;
            var blob = BlobStorage.GetBlobXml(ContainerName, BlobName, out ignored);
            BlobStorage.DeleteBlobIfExist(ContainerName, BlobName);

            Assert.IsTrue(blob.HasValue);
            var xml = blob.Value;
            var property = xml.Elements().Single();
            Assert.AreEqual(data.MyGuid, new Guid(property.Value));
        }

        private string CreateNewBlob()
        {
            var name = "x" + Guid.NewGuid().ToString("N");
            BlobStorage.PutBlob(ContainerName, name, name);
            return name;
        }

        [Test]
        public void CanAcquireBlobLease()
        {
            var blobName = CreateNewBlob();
            var result = BlobStorage.TryAcquireLease(ContainerName, blobName);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNullOrEmpty(result.Value);

            // cleanup
            BlobStorage.TryReleaseLease(ContainerName, blobName, result.Value);
        }

        [Test]
        public void CanNotAcquireBlobLeaseOnLockedBlob()
        {
            var blobName = CreateNewBlob();
            var result = BlobStorage.TryAcquireLease(ContainerName, blobName);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNullOrEmpty(result.Value);
            var leaseId = result.Value;

            // Second trial should fail
            result = BlobStorage.TryAcquireLease(ContainerName, blobName);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Conflict", result.Error);

            // cleanup
            BlobStorage.TryReleaseLease(ContainerName, blobName, leaseId);
        }

        [Test]
        public void CanReleaseLockedBlobWithMatchingLeaseId()
        {
            var blobName = CreateNewBlob();
            var lease = BlobStorage.TryAcquireLease(ContainerName, blobName);
            Assert.IsTrue(BlobStorage.TryReleaseLease(ContainerName, blobName, lease.Value).IsSuccess);
        }

        [Test]
        public void CanNotReleaseLockedBlobWithoutMatchingLeaseId()
        {
            var blobName = CreateNewBlob();
            var result = BlobStorage.TryAcquireLease(ContainerName, blobName);
            Assert.IsFalse(BlobStorage.TryReleaseLease(ContainerName, blobName, Guid.NewGuid().ToString("N")).IsSuccess);

            // cleanup
            BlobStorage.TryReleaseLease(ContainerName, blobName, result.Value);
        }

        [Test]
        public void CanNotReleaseUnleasedBlob()
        {
            var blobName = CreateNewBlob();
            Assert.IsFalse(BlobStorage.TryReleaseLease(ContainerName, blobName, Guid.NewGuid().ToString("N")).IsSuccess);
        }
    }

    [Serializable]
    internal class MyBlob
    {
        public Guid MyGuid { get; private set; }

        public MyBlob()
        {
            MyGuid = Guid.NewGuid();
        }
    }
}
