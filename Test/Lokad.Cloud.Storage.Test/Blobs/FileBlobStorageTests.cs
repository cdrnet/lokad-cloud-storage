#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Storage.FileSystem;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test.Blobs
{
    [TestFixture]
    [Category("FileStorage")]
    public class FileBlobStorageTests : BlobStorageTests
    {
        const string ContainerName1 = "container-1";
        const string ContainerName2 = "container-2";
        const string ContainerName3 = "container-3";

        public FileBlobStorageTests()
            : base(new FileBlobStorageProvider(Path.Combine(Environment.CurrentDirectory, "TestData"), new CloudFormatter()))
        {
        }

        [TearDown]
        public void TearDown()
        {
            BlobStorage.DeleteContainerIfExist(ContainerName1);
            BlobStorage.DeleteContainerIfExist(ContainerName2);
            BlobStorage.DeleteContainerIfExist(ContainerName3);
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            var dir = Path.Combine(Environment.CurrentDirectory, "TestData");
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }

        //[Test]
        //public override void PutBlobEnforceMatchingEtagAsync()
        //{
        //    BlobStorage.PutBlob(ContainerName, BlobName, 1);

        //    // File provider etags are not perfect
        //    Thread.Sleep(10);

        //    var etag = BlobStorage.GetBlobEtag(ContainerName, BlobName);
        //    var task = BlobStorage.PutBlobAsync(ContainerName, BlobName, 2, Guid.NewGuid().ToString());
        //    task.Wait();

        //    Assert.IsTrue(task.IsCompleted);
        //    Assert.IsNull(task.Result, "#A00 Blob shouldn't be updated if etag is not matching");

        //    task = BlobStorage.PutBlobAsync(ContainerName, BlobName, 3, etag);
        //    task.Wait();

        //    Assert.IsTrue(task.IsCompleted);
        //    Assert.IsNotNull(task.Result, "#A01 Blob should have been updated");
        //}

        //[Test]
        //public override void EtagChangesWithBlobChange()
        //{
        //    BlobStorage.PutBlob(ContainerName, BlobName, 1);
        //    var etag = BlobStorage.GetBlobEtag(ContainerName, BlobName);

        //    // File provider etags are not perfect
        //    Thread.Sleep(1);

        //    BlobStorage.PutBlob(ContainerName, BlobName, 1);
        //    var newEtag = BlobStorage.GetBlobEtag(ContainerName, BlobName);
        //    Assert.AreNotEqual(etag, newEtag, "#A00.");
        //}

        [Test]
        public void BlobsGetCreatedMonoThread()
        {
            const string blobPrefix = "mockBlobPrefix";
            const string secondBlobPrefix = "sndBlobPrefix";

            BlobStorage.CreateContainerIfNotExist(ContainerName1);
            BlobStorage.CreateContainerIfNotExist(ContainerName2);
            BlobStorage.CreateContainerIfNotExist(ContainerName3);

            BlobStorage.PutBlob(ContainerName1, blobPrefix + "/" + "blob1", new DateTime(2009, 08, 27));
            BlobStorage.PutBlob(ContainerName1, blobPrefix + "/" + "blob2", new DateTime(2009, 08, 28));
            BlobStorage.PutBlob(ContainerName1, blobPrefix + "/" + "blob3", new DateTime(2009, 08, 29));
            BlobStorage.PutBlob(ContainerName2, blobPrefix + "/" + "blob2", new DateTime(1984, 07, 06));
            BlobStorage.PutBlob(ContainerName1, secondBlobPrefix + "/" + "blob1", new DateTime(2009, 08, 30));

            Assert.AreEqual(
                3,
                BlobStorage.ListBlobNames(ContainerName1, blobPrefix).Count(),
                "first container with first prefix does not hold 3 blobs");

            Assert.AreEqual(
                1,
                BlobStorage.ListBlobNames(ContainerName2, blobPrefix).Count(),
                "second container with first prefix does not hold 1 blobs");

            Assert.AreEqual(
                0,
                BlobStorage.ListBlobNames(ContainerName3, blobPrefix).Count(),
                "third container with first prefix does not hold 0 blob");

            Assert.AreEqual(
                1,
                BlobStorage.ListBlobNames(ContainerName1, secondBlobPrefix).Count(),
                "first container with second prefix does not hold 1 blobs");
        }

        [Test]
        public void BlobsGetCreatedParallel()
        {
            const string blobPrefix = "mockBlobPrefix";

            BlobStorage.CreateContainerIfNotExist(ContainerName1);
            BlobStorage.CreateContainerIfNotExist(ContainerName2);

            Parallel.For(0, 32, new ParallelOptions { MaxDegreeOfParallelism = 32 }, k =>
                {
                    var container = k <= 15 ? ContainerName1 : ContainerName2;
                    var random = new Random(Guid.NewGuid().GetHashCode());
                    for (int i = 0; i < 100; i++)
                    {
                        BlobStorage.PutBlob(
                            container,
                            "mockBlobPrefix" + k + "/blob" + i,
                            random.NextDouble());
                    }
                });

            Assert.AreEqual(
                1600,
                BlobStorage.ListBlobNames(ContainerName1, blobPrefix).Count(),
                "first container with corresponding prefix does not hold 3 blobs");

            Assert.AreEqual(
                1600,
                BlobStorage.ListBlobNames(ContainerName2, blobPrefix).Count(),
                "second container with corresponding prefix does not hold 1 blobs");
        }
    }
}
