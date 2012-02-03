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
    /// Full custom document set with compressed BinaryWriter serialization
    /// </summary>
    public class CustomMyDocumentSet : CompressedBinaryDocumentSet<MyDocument, int>
    {
        public CustomMyDocumentSet(IBlobStorageProvider blobs)
            : base(blobs, KeyToLocation, () => new BlobLocation("document-container", ""))
        {
            Serializer = this;
        }

        private static IBlobLocation KeyToLocation(int key)
        {
            return new BlobLocation("document-container", key.ToString());
        }

        public override IEnumerable<int> ListAllKeys()
        {
            return Blobs.ListBlobNames("document-container").Select(Int32.Parse);
        }

        protected override void Serialize(MyDocument document, BinaryWriter writer)
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
        }

        protected override MyDocument Deserialize(BinaryReader reader)
        {
            int textBytesCount = reader.ReadInt32();
            var textBytes = reader.ReadBytes(textBytesCount);
            var text = Encoding.UTF8.GetString(textBytes);
            return new MyDocument { ArbitraryString = text };
        }
    }
}
