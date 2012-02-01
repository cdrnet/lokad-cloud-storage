#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage.Documents;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test.Documents
{
    [TestFixture]
    public abstract class DocumentSetTests
    {
        protected abstract IDocumentSet<MyDocument, int> BuildDocumentSet();

        [Test]
        public void Can_get_insert_and_replace()
        {
            var set = BuildDocumentSet();
            MyDocument document;

            Assert.IsFalse(set.TryGet(20, out document));

            set.InsertOrReplace(20, new MyDocument { ArbitraryString = "B" });
            Assert.IsTrue(set.TryGet(20, out document));
            Assert.AreEqual("B", document.ArbitraryString);

            set.InsertOrReplace(20, new MyDocument { ArbitraryString = "C" });
            Assert.IsTrue(set.TryGet(20, out document));
            Assert.AreEqual("C", document.ArbitraryString);
        }

        [Test]
        public void Can_delete_if_exist()
        {
            var set = BuildDocumentSet();

            Assert.IsFalse(set.DeleteIfExist(20));

            set.InsertOrReplace(20, new MyDocument { ArbitraryString = "A" });
            Assert.IsTrue(set.DeleteIfExist(20));
            Assert.IsFalse(set.DeleteIfExist(20));
        }

        [Test]
        public void UpdateIfExist_should_update_but_not_insert()
        {
            var set = BuildDocumentSet();
            MyDocument document;

            Assert.IsFalse(set.TryGet(20, out document));

            set.UpdateIfExist(20, d => new MyDocument { ArbitraryString = "D0" });
            Assert.IsFalse(set.TryGet(20, out document));

            set.InsertOrReplace(20, new MyDocument { ArbitraryString = "D1" });
            Assert.IsTrue(set.TryGet(20, out document));
            Assert.AreEqual("D1", document.ArbitraryString);

            set.UpdateIfExist(20, d => new MyDocument { ArbitraryString = "D2" });
            Assert.IsTrue(set.TryGet(20, out document));
            Assert.AreEqual("D2", document.ArbitraryString);
        }

        [Test]
        public void Update_should_update_default_value_if_not_exist()
        {
            var set = BuildDocumentSet();

            var document = set.Update(20,
                d => new MyDocument { ArbitraryString = d.ArbitraryString + "+" },
                () => new MyDocument { ArbitraryString = "E" });
            Assert.AreEqual("E+", document.ArbitraryString);

            document = set.Update(20,
                d => new MyDocument { ArbitraryString = d.ArbitraryString + "+" },
                () => new MyDocument { ArbitraryString = "E" });
            Assert.AreEqual("E++", document.ArbitraryString);
        }

        [Test]
        public void UpdateOrInsert_should_not_update_insert_value_if_not_exist()
        {
            var set = BuildDocumentSet();

            var document = set.UpdateOrInsert(20,
                d => new MyDocument { ArbitraryString = d.ArbitraryString + "+" },
                () => new MyDocument { ArbitraryString = "F" });
            Assert.AreEqual("F", document.ArbitraryString);

            document = set.UpdateOrInsert(20,
                d => new MyDocument { ArbitraryString = d.ArbitraryString + "+" },
                () => new MyDocument { ArbitraryString = "F" });
            Assert.AreEqual("F+", document.ArbitraryString);
        }
    }
}
