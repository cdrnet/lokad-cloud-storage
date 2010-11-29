#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Runtime.Serialization;
using Lokad.Cloud;
using Lokad.Cloud.Storage;

namespace SimpleBlob
{
    [DataContract]
    class Book
    {
        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Author { get; set; }
    }

    class BookName : BlobName<Book>
    {
        public override string ContainerName
        {
            get { return "books"; } // default container for 'Book' entities
        }

        [Rank(0)] public string Publisher { get; set;}

        // TreatDefaultAsNull = true, '0' will be ignored
        [Rank(1, true)] public int BookId { get; set;}
    }

    class Program
    {
        static void Main(string[] args)
        {            
            // TODO: change your connection string here
            var providers = Standalone.CreateProviders("DefaultEndpointsProtocol=https;AccountName=;AccountKey=");
            var blobs = providers.BlobStorage;

            var potterBook = new Book { Author = "J. K. Rowling", Title = "Harry Potter" };
            // Resulting blob name is: Bloomsbury Publishing/1
            var potterRef = new BookName {Publisher = "Bloomsbury Publishing", BookId = 1};

            var poemsBook = new Book { Author = "John Keats", Title = "Complete Poems" };
            // Resulting blob name is: Harvard University Press/2
            var poemsRef = new BookName {Publisher = "Harvard University Press", BookId = 2};
            
            // writing entities to the storage
            blobs.PutBlob(potterRef, potterBook);
            blobs.PutBlob(poemsRef, poemsBook);

            // retrieving all entities from 'Bloomsbury Publishing'
            foreach (var book in blobs.ListBlobs(new BookName { Publisher = "Bloomsbury Publishing" }))
            {
                Console.WriteLine("{0} by {1}", book.Title, book.Author);
            }

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }
    }
}
