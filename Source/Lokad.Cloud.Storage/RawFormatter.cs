#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;

namespace Lokad.Cloud.Storage
{
    /// <summary>
    /// Raw byte pass-through formatter, supporting byte arrays only.
    /// </summary>
    public class RawFormatter : IDataSerializer
    {
        /// <remarks>Supports byte[] only</remarks>
        public void Serialize(object instance, Stream destination, Type type)
        {
            var data = instance as byte[];
            if (data == null || type != typeof(byte[]))
            {
                throw new NotSupportedException();
            }

            destination.Write(data, 0, data.Length);
        }

        /// <remarks>Supports byte[] only</remarks>
        public object Deserialize(Stream source, Type type)
        {
            if (type != typeof(byte[]))
            {
                throw new NotSupportedException();
            }

            // shortcut if source is already a memory stream
            var memorySource = source as MemoryStream;
            if (memorySource != null)
            {
                return memorySource.ToArray();
            }

            using (var memoryStream = new MemoryStream())
            {
                source.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}