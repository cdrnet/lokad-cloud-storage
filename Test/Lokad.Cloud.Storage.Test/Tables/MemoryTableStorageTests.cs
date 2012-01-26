#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test.Tables
{
    /// <remarks>Includes all unit tests for the real table provider</remarks>
    [TestFixture]
    [Category("InMemoryStorage")]
    public class MemoryTableStorageTests : TableStorageTests
    {
        public MemoryTableStorageTests()
            : base(CloudStorage.ForInMemoryStorage().BuildStorageProviders())
        {
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var tableName in TableStorage.GetTables().Distinct().ToList())
            {
                TableStorage.DeleteTable(tableName);
            }
        }

        [Test]
        public void CreateAndGetTable()
        {
            var originalCount = TableStorage.GetTables().Count();

            //Single thread.
            for (int i = 0; i <= 5; i++)
            {
                TableStorage.CreateTable("table" + i.ToString());
            }

            Assert.AreEqual(6, TableStorage.GetTables().Count() - originalCount, "#A01");

            //Remove tables.
            Assert.False(TableStorage.DeleteTable("Table_that_does_not_exist"), "#A02");
            var isSuccess = TableStorage.DeleteTable("table" + 4.ToString());

            Assert.IsTrue(isSuccess, "#A03");
            Assert.AreEqual(5, TableStorage.GetTables().Count() - originalCount, "#A04");
        }

        [Test]
        public void InsertAndGetMethodSingleThread()
        {
            const string tableName = "myTable";

            TableStorage.CreateTable(tableName);

            const int partitionCount = 10;

            //Creating entities: a hundred. Pkey created with the last digit of a number between 0 and 99.
            var entities =
                Enumerable.Range(0, 100).Select(
                    i =>
                    new CloudEntity<object>
                        {
                            PartitionKey = "Pkey-" + (i % partitionCount).ToString("0"),
                            RowKey = "RowKey-" + i.ToString("00"),
                            Value = new object()
                        });

            //Insert entities.
            TableStorage.Insert(tableName, entities);

            //retrieve all of them.
            var retrievedEntities1 = TableStorage.Get<object>(tableName);
            Assert.AreEqual(100, retrievedEntities1.Count(), "#B01");

            //Test overloads...
            var retrievedEntites2 = TableStorage.Get<object>(tableName, "Pkey-9");
            Assert.AreEqual(10, retrievedEntites2.Count(), "#B02");

            var retrievedEntities3 = TableStorage.Get<object>(
                tableName, "Pkey-7", new[] { "RowKey-27", "RowKey-37", "IAmNotAKey" });

            Assert.AreEqual(2, retrievedEntities3.Count(), "#B03");

            //The following tests handle the exclusive and inclusive bounds of key search.
            var retrieved4 = TableStorage.Get<object>(tableName, "Pkey-1", "RowKey-01", "RowKey-91");
            Assert.AreEqual(9, retrieved4.Count(), "#B04");

            var retrieved5 = TableStorage.Get<object>(tableName, "Pkey-1", "RowKey-01", null);
            Assert.AreEqual(10, retrieved5.Count(), "#B05");

            var retrieved6 = TableStorage.Get<object>(tableName, "Pkey-1", null, null);
            Assert.AreEqual(10, retrieved6.Count(), "#B06");

            var retrieved7 = TableStorage.Get<object>(tableName, "Pkey-1", null, "RowKey-21");
            Assert.AreEqual(2, retrieved7.Count(), "#B07");

            //The next test should handle non existing table names.
            //var isSuccess = false;

            // TODO: Looks like something is not finished here

            var emptyEnumeration = TableStorage.Get<object>("IAmNotATable", "IaMNotAPartiTion");

            Assert.AreEqual(0, emptyEnumeration.Count(), "#B08");
        }

        [Test]
        public void InsertUpdateAndDeleteSingleThread()
        {
            const string tableName = "myTable";
            const string newTableName = "myNewTable";

            TableStorage.CreateTable(tableName);

            const int partitionCount = 10;

            var entities =
                Enumerable.Range(0, 100).Select(
                    i =>
                    new CloudEntity<object>
                        {
                            PartitionKey = "Pkey-" + (i % partitionCount).ToString("0"),
                            RowKey = "RowKey-" + i.ToString("00"),
                            Value = new object()
                        });
            TableStorage.Insert(tableName, entities);

            var isSucces = false;
            try
            {
                TableStorage.Insert(
                    tableName, new[] { new CloudEntity<object> { PartitionKey = "Pkey-6", RowKey = "RowKey-56" } });
            }
            catch (Exception exception)
            {
                isSucces = (exception as InvalidOperationException) == null ? false : true;
            }
            Assert.IsTrue(isSucces);

            TableStorage.CreateTable(newTableName);
            TableStorage.Insert(
                newTableName,
                new[]
                    { new CloudEntity<object> { PartitionKey = "Pkey-6", RowKey = "RowKey-56", Value = new object() } });

            Assert.AreEqual(2, TableStorage.GetTables().Count());

            TableStorage.Update(
                newTableName,
                new[] { new CloudEntity<object> { PartitionKey = "Pkey-6", RowKey = "RowKey-56", Value = 2000 } },
                true);
            Assert.AreEqual(
                2000, (int)TableStorage.Get<object>(newTableName, "Pkey-6", new[] { "RowKey-56" }).First().Value);

            TableStorage.Delete<object>(newTableName, "Pkey-6", new[] { "RowKey-56" });

            var retrieved = TableStorage.Get<object>(newTableName);
            Assert.AreEqual(0, retrieved.Count());
        }

        [Test]
        public void CreateAndGetTablesMultipleTasks()
        {
            //Multi thread.
            const int M = 32;

            Task.WaitAll(Enumerable.Range(0, M)
                .Select(i => Task.Factory.StartNew(() =>
                    {
                        for (int k1 = 0; k1 < 10; k1++)
                        {
                            TableStorage.CreateTable("table" + k1.ToString());
                        }
                    }))
                .ToArray());

            Assert.AreEqual(10, TableStorage.GetTables().Distinct().Count());
        }
    }
}
