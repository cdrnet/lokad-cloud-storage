#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lokad.Cloud.Storage.Documents;

namespace Lokad.Cloud.Storage.Test.Documents
{
    /// <summary>
    /// Full custom document set with ad-hoc serialization
    /// </summary>
    public class CustomMyDocumentSet : EnumerableDocumentSet<MyDocument, int, object>, IDataSerializer
    {
        public CustomMyDocumentSet(IBlobStorageProvider blobs)
            : base(blobs, KeyToLocation, prefix => new BlobLocation("document-container", ""))
        {
            Serializer = this;
        }

        private static IBlobLocation KeyToLocation(int key)
        {
            return new BlobLocation("document-container", key.ToString());
        }

        public override IEnumerable<int> ListAllKeys(object prefix = null)
        {
            return Blobs.ListBlobNames("document-container").Select(Int32.Parse);
        }

        void IDataSerializer.Serialize(object instance, Stream destinationStream, Type type)
        {
            var document = instance as MyDocument;
            if (instance == null)
            {
                throw new NotSupportedException();
            }

            using (var buffered = new BufferedStream(destinationStream, 4 * 1024))
            using (var writer = new BinaryWriter(buffered))
            {
                if (string.IsNullOrEmpty(document.ArbitraryString))
                {
                    writer.Write(0);
                }
                else
                {
                    var stringBytes = Encoding.UTF8.GetBytes(document.ArbitraryString);
                    writer.Write(stringBytes.Length);
                    writer.Write(stringBytes);
                }

                writer.Flush();
                buffered.Flush();
            }
        }

        object IDataSerializer.Deserialize(Stream sourceStream, Type type)
        {
            if (type != typeof(MyDocument))
            {
                throw new NotSupportedException();
            }

            using (var reader = new BinaryReader(sourceStream))
            {
                int textBytesCount = reader.ReadInt32();
                var textBytes = reader.ReadBytes(textBytesCount);
                var text = Encoding.UTF8.GetString(textBytes);

                return new MyDocument { ArbitraryString = text };
            }
        }
    }
}
