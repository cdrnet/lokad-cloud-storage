#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using Lokad.Serialization;

namespace Lokad.Cloud.Storage
{
    internal static class DataSerializerExtensions
    {
        public static Result<T, Exception> TryDeserializeAs<T>(this IDataSerializer serializer, Stream source)
        {
            var position = source.Position;
            try
            {
                var result = serializer.Deserialize(source, typeof(T));
                return result is T
                    ? Result<T, Exception>.CreateSuccess((T) result)
                    : Result<T, Exception>.CreateError(new InvalidCastException(
                        String.Format("Source was expected to be of type {0} but was of type {1}.",
                            typeof (T).Name,
                            result.GetType().Name)));
            }
            catch (Exception e)
            {
                return Result<T, Exception>.CreateError(e);
            }
            finally
            {
                source.Position = position;
            }
        }

        public static Result<T, Exception> TryDeserializeAs<T>(this IDataSerializer serializer, byte[] source)
        {
            using (var stream = new MemoryStream(source))
            {
                return TryDeserializeAs<T>(serializer, stream);
            }
        }
    }
}
