#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Linq;
using Lokad.Cloud.Storage.Documents;
using Lokad.Cloud.Storage.InMemory;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test.Documents
{
    [TestFixture]
    public class CustomMyDocumentSetTests : DocumentSetTests
    {
        protected override IDocumentSet<MyDocument, int> BuildDocumentSet()
        {
            var blobs = new MemoryBlobStorageProvider();
            return new CustomMyDocumentSet(blobs);
        }

        [Test]
        public void Can_list_all_keys()
        {
            var blobs = new MemoryBlobStorageProvider();
            var set = new CustomMyDocumentSet(blobs);

            Assert.IsFalse(set.ListAllKeys().Any());

            set.InsertOrReplace(1, new MyDocument { ArbitraryString = "X1" });
            set.InsertOrReplace(2, new MyDocument { ArbitraryString = "X2" });
            set.InsertOrReplace(3, new MyDocument { ArbitraryString = "X3" });
            Assert.AreEqual(3, set.ListAllKeys().Count());
            Assert.AreEqual(1, set.ListAllKeys().First());
            Assert.AreEqual(3, set.ListAllKeys().Last());
        }

        [Test]
        public void Can_get_all_documents()
        {
            var blobs = new MemoryBlobStorageProvider();
            var set = new CustomMyDocumentSet(blobs);

            Assert.IsFalse(set.GetAll().Any());

            set.InsertOrReplace(1, new MyDocument { ArbitraryString = "X1" });
            set.InsertOrReplace(2, new MyDocument { ArbitraryString = "X2" });
            set.InsertOrReplace(3, new MyDocument { ArbitraryString = "X3" });
            Assert.AreEqual(3, set.GetAll().Count());
            Assert.AreEqual("X1", set.GetAll().First().ArbitraryString);
            Assert.AreEqual("X3", set.GetAll().Last().ArbitraryString);
        }

        [Test]
        public void Can_delet_all_documents()
        {
            var blobs = new MemoryBlobStorageProvider();
            var set = new CustomMyDocumentSet(blobs);

            Assert.IsFalse(set.ListAllKeys().Any());

            set.InsertOrReplace(1, new MyDocument { ArbitraryString = "X1" });
            set.InsertOrReplace(2, new MyDocument { ArbitraryString = "X2" });
            set.InsertOrReplace(3, new MyDocument { ArbitraryString = "X3" });
            Assert.AreEqual(3, set.ListAllKeys().Count());

            set.DeleteIfExist(2);
            Assert.AreEqual(2, set.ListAllKeys().Count());

            set.DeleteAll();
            Assert.AreEqual(0, set.ListAllKeys().Count());
        }
    }
}
