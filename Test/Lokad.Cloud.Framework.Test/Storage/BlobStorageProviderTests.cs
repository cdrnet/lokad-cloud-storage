#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using Autofac;
using Lokad.Cloud.Shared.Test;
using Lokad.Cloud.Test;
using NUnit.Framework;

// TODO: refactor tests so that containers do not have to be created each time.

namespace Lokad.Cloud.Storage.Test
{
    [TestFixture]
    public class BlobStorageProviderTests
    {
        private const string ContainerName = "tests-blobstorageprovider-mycontainer";
        private const string BlobName = "myprefix/myblob";

        private readonly IBlobStorageProvider _blobStorage;

        public BlobStorageProviderTests()
        {
            _blobStorage = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
        }

        protected BlobStorageProviderTests(IBlobStorageProvider blobStorageProvider)
        {
            _blobStorage = blobStorageProvider;
        }

        [TestFixtureSetUp]
        public void Setup()
        {
            _blobStorage.CreateContainerIfNotExist(ContainerName);
            _blobStorage.DeleteBlobIfExist(ContainerName, BlobName);
        }

        [Test]
        public void CanListContainers()
        {
            Assert.IsTrue(_blobStorage.ListContainers().Contains(ContainerName));
            Assert.IsTrue(_blobStorage.ListContainers(ContainerName.Substring(0, 5)).Contains(ContainerName));
            Assert.IsFalse(_blobStorage.ListContainers("another-prefix").Contains(ContainerName));
        }

        [Test]
        public void GetAndDelete()
        {
            _blobStorage.DeleteBlobIfExist(ContainerName, BlobName);
            Assert.IsFalse(_blobStorage.GetBlob<int>(ContainerName, BlobName).HasValue, "#A00");
        }

        [Test]
        public void BlobHasEtag()
        {
            _blobStorage.PutBlob(ContainerName, BlobName, 1);
            var etag = _blobStorage.GetBlobEtag(ContainerName, BlobName);
            Assert.IsNotNull(etag, "#A00");
        }

        [Test]
        public void MissingBlobHasNoEtag()
        {
            _blobStorage.DeleteBlobIfExist(ContainerName, BlobName);
            var etag = _blobStorage.GetBlobEtag(ContainerName, BlobName);
            Assert.IsNull(etag, "#A00");
        }

        [Test]
        public void PutBlobEnforceNoOverwrite()
        {
            _blobStorage.PutBlob(ContainerName, BlobName, 1);

            string etag;
            var isSaved = _blobStorage.PutBlob(ContainerName, BlobName, 6, false, out etag);
            Assert.IsFalse(isSaved, "#A00");
            Assert.IsNull(etag, "#A01");

            Assert.IsTrue(_blobStorage.GetBlob<int>(ContainerName, BlobName).HasValue, "#A02");
            Assert.AreEqual(1, _blobStorage.GetBlob<int>(ContainerName, BlobName).Value, "#A03");
        }

        [Test]
        public void PutBlobEnforceOverwrite()
        {
            _blobStorage.PutBlob(ContainerName, BlobName, 1);

            string etag;
            var isSaved = _blobStorage.PutBlob(ContainerName, BlobName, 6, true, out etag);
            Assert.IsTrue(isSaved, "#A00");
            Assert.IsNotNull(etag, "#A01");

            var maybe = _blobStorage.GetBlob<int>(ContainerName, BlobName);
            Assert.IsTrue(maybe.HasValue, "#A02");
            Assert.AreEqual(6, maybe.Value, "#A03");
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

                _blobStorage.PutBlob(ContainerName, BlobName, buffer);
                var maybe = _blobStorage.GetBlob<byte[]>(ContainerName, BlobName);

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
            _blobStorage.PutBlob(ContainerName, BlobName, 1);

            var etag = _blobStorage.GetBlobEtag(ContainerName, BlobName);
            var isUpdated = _blobStorage.PutBlob(ContainerName, BlobName, 2, Guid.NewGuid().ToString());

            Assert.IsTrue(!isUpdated, "#A00 Blob shouldn't be updated if etag is not matching");

            isUpdated = _blobStorage.PutBlob(ContainerName, BlobName, 3, etag);
            Assert.IsTrue(isUpdated, "#A01 Blob should have been updated");
        }

        [Test]
        public void EtagChangesOnlyWithBlogChange()
        {
            _blobStorage.PutBlob(ContainerName, BlobName, 1);
            var etag = _blobStorage.GetBlobEtag(ContainerName, BlobName);
            var newEtag = _blobStorage.GetBlobEtag(ContainerName, BlobName);
            Assert.AreEqual(etag, newEtag, "#A00");
        }

        [Test]
        public void EtagChangesWithBlogChange()
        {
            _blobStorage.PutBlob(ContainerName, BlobName, 1);
            var etag = _blobStorage.GetBlobEtag(ContainerName, BlobName);
            _blobStorage.PutBlob(ContainerName, BlobName, 1);
            var newEtag = _blobStorage.GetBlobEtag(ContainerName, BlobName);
            Assert.AreNotEqual(etag, newEtag, "#A00.");
        }

        [Test]
        public void GetBlobIfNotModifiedNoChangeNoRetrieval()
        {
            _blobStorage.PutBlob(ContainerName, BlobName, 1);
            var etag = _blobStorage.GetBlobEtag(ContainerName, BlobName);

            string newEtag;
            var output = _blobStorage.GetBlobIfModified<MyBlob>(ContainerName, BlobName, etag, out newEtag);

            Assert.IsNull(newEtag, "#A00");
            Assert.IsFalse(output.HasValue, "#A01");
        }

        [Test]
        public void GetBlobIfNotModifiedWithTypeMistmatch()
        {
            _blobStorage.PutBlob(ContainerName, BlobName, 1); // pushing Int32

            string newEtag; // pulling MyBlob
            var output = _blobStorage.GetBlobIfModified<MyBlob>(ContainerName, BlobName, "dummy", out newEtag);
            Assert.IsFalse(output.HasValue);
        }

        /// <remarks>
        /// This test does not check the behavior in case of concurrency stress.
        /// </remarks>
        [Test]
        public void UpsertBlockOrSkipNoStress()
        {
            var blobName = "test" + Guid.NewGuid().ToString("N");
            Assert.IsFalse(_blobStorage.GetBlob<int>(ContainerName, blobName).HasValue);

            int inserted = 0, updated = 10;

// ReSharper disable AccessToModifiedClosure

            // skip insert
            Assert.IsFalse(_blobStorage.UpsertBlobOrSkip(ContainerName, blobName, () => Maybe<int>.Empty, x => ++updated).HasValue);
            Assert.AreEqual(0, inserted);
            Assert.AreEqual(10, updated);
            Assert.IsFalse(_blobStorage.GetBlob<int>(ContainerName, blobName).HasValue);

            // do insert
            Assert.IsTrue(_blobStorage.UpsertBlobOrSkip<int>(ContainerName, blobName, () => ++inserted, x => ++updated).HasValue);
            Assert.AreEqual(1, inserted);
            Assert.AreEqual(10, updated);
            Assert.AreEqual(1, _blobStorage.GetBlob<int>(ContainerName, blobName).Value);

            // skip update
            Assert.IsFalse(_blobStorage.UpsertBlobOrSkip<int>(ContainerName, blobName, () => ++inserted, x => Maybe<int>.Empty).HasValue);
            Assert.AreEqual(1, inserted);
            Assert.AreEqual(10, updated);
            Assert.AreEqual(1, _blobStorage.GetBlob<int>(ContainerName, blobName).Value);

            // do update
            Assert.IsTrue(_blobStorage.UpsertBlobOrSkip<int>(ContainerName, blobName, () => ++inserted, x => ++updated).HasValue);
            Assert.AreEqual(1, inserted);
            Assert.AreEqual(11, updated);
            Assert.AreEqual(11, _blobStorage.GetBlob<int>(ContainerName, blobName).Value);

            // cleanup
            _blobStorage.DeleteBlobIfExist(ContainerName, blobName);

// ReSharper restore AccessToModifiedClosure
        }

        /// <remarks>
        /// Loose check of the behavior under concurrency stress.
        /// </remarks>
        [Test]
        public void UpsertBlockOrSkipWithStress()
        {
            _blobStorage.PutBlob(ContainerName, BlobName, 0);

            var array = new Maybe<int>[8];
            array = array
                .AsParallel()
                .Select(k => _blobStorage.UpsertBlobOrSkip<int>(ContainerName, BlobName, () => 1, i => i + 1))
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

        // TODO: CreatePutGetRangeDelete is way too complex as a unit test

        [Test]
        public void CreatePutGetRangeDelete()
        {
            var privateContainerName = "test-" + Guid.NewGuid().ToString("N");

            _blobStorage.CreateContainerIfNotExist(privateContainerName);

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
                _blobStorage.PutBlob(privateContainerName, blobNames[i], inputBlobs[i]);
            }

            string[] allEtags;
            var allBlobs = _blobStorage.GetBlobRange<MyBlob>(privateContainerName, blobNames, out allEtags);

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

            allBlobs = _blobStorage.GetBlobRange<MyBlob>(privateContainerName, wrongBlobNames, out allEtags);

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

            _blobStorage.DeleteContainerIfExist(privateContainerName);
        }


        [Test]
        public void NullableType_Default()
        {
            var privateContainerName = "test-" + Guid.NewGuid().ToString("N");

            _blobStorage.CreateContainerIfNotExist(privateContainerName);

            int? value1 = 10;
            int? value2 = null;

            _blobStorage.PutBlob(privateContainerName, "test1", value1);
            _blobStorage.PutBlob(privateContainerName, "test2", value1);

            var output1 = _blobStorage.GetBlob<int?>(privateContainerName, "test1");
            var output2 = _blobStorage.GetBlob<int?>(privateContainerName, "test2");

            Assert.AreEqual(value1.Value, output1.Value);
            Assert.IsFalse(value2.HasValue);

            _blobStorage.DeleteContainerIfExist(privateContainerName);
        }

        [Test]
        public void ListBlobNames()
        {
            var prefix = Guid.NewGuid().ToString("N");

            var prefixed = Range.Array(10).Select(i => prefix + Guid.NewGuid().ToString("N")).ToArray();
            var unprefixed = Range.Array(13).Select(i => Guid.NewGuid().ToString("N")).ToArray();

            foreach (var n in prefixed)
            {
                _blobStorage.PutBlob(ContainerName, n, n);
            }

            foreach (var n in unprefixed)
            {
                _blobStorage.PutBlob(ContainerName, n, n);
            }

            var list = _blobStorage.ListBlobNames(ContainerName, prefix).ToArray();

            Assert.AreEqual(prefixed.Length, list.Length, "#A00");

            foreach (var n in list)
            {
                Assert.IsTrue(prefixed.Contains(n), "#A01");
                Assert.IsFalse(unprefixed.Contains(n), "#A02");
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
                _blobStorage.PutBlob(ContainerName, n, n);
            }

            foreach (var n in unprefixed)
            {
                _blobStorage.PutBlob(ContainerName, n, n);
            }

            var list = _blobStorage.ListBlobs<string>(ContainerName, prefix).ToArray();

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
            _blobStorage.PutBlob(ContainerName, BlobName, data, true);

            string ignored;
            var blob = _blobStorage.GetBlobXml(ContainerName, BlobName, out ignored);
            _blobStorage.DeleteBlobIfExist(ContainerName, BlobName);

            Assert.IsTrue(blob.HasValue);
            var xml = blob.Value;
            var property = xml.Elements().Single();
            Assert.AreEqual(data.MyGuid, new Guid(property.Value));
        }

        private string CreateNewBlob()
        {
            var name = "x" + Guid.NewGuid().ToString("N");
            _blobStorage.PutBlob(ContainerName, name, name);
            return name;
        }

        [Test]
        public void CanAcquireBlobLease()
        {
            var blobName = CreateNewBlob();
            var result = _blobStorage.TryAcquireLease(ContainerName, blobName);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNullOrEmpty(result.Value);
        }

        [Test]
        public void CanNotAcquireBlobLeaseOnLockedBlob()
        {
            var blobName = CreateNewBlob();
            var result = _blobStorage.TryAcquireLease(ContainerName, blobName);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNullOrEmpty(result.Value);

            // Second trial should fail
            result = _blobStorage.TryAcquireLease(ContainerName, blobName);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Conflict", result.Error);
        }

        [Test]
        public void CanReleaseLockedBlobWithMatchingLeaseId()
        {
            var blobName = CreateNewBlob();
            var lease = _blobStorage.TryAcquireLease(ContainerName, blobName);
            Assert.IsTrue(_blobStorage.TryReleaseLease(ContainerName, blobName, lease.Value));
        }

        [Test]
        public void CanNotReleaseLockedBlobWithoutMatchingLeaseId()
        {
            var blobName = CreateNewBlob();
            _blobStorage.TryAcquireLease(ContainerName, blobName);
            Assert.IsFalse(_blobStorage.TryReleaseLease(ContainerName, blobName, Guid.NewGuid().ToString("N")));
        }

        [Test]
        public void CanNotReleaseUnleasedBlob()
        {
            var blobName = CreateNewBlob();
            Assert.IsFalse(_blobStorage.TryReleaseLease(ContainerName, blobName, Guid.NewGuid().ToString("N")));
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
