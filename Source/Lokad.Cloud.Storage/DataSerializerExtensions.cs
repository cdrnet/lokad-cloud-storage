#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Lokad.Cloud.Storage.Shared;

namespace Lokad.Cloud.Storage
{
    internal static class DataSerializerExtensions
    {
        public static Shared.Monads.Result<T, Exception> TryDeserializeAs<T>(this IDataSerializer serializer, Stream source)
        {
            var position = source.Position;
            try
            {
                var result = serializer.Deserialize(source, typeof(T));
                if (result == null)
                {
                    return Shared.Monads.Result<T, Exception>.CreateError(new SerializationException("Serializer returned null"));
                }

                if (!(result is T))
                {
                    return Shared.Monads.Result<T, Exception>.CreateError(new InvalidCastException(
                        String.Format("Source was expected to be of type {0} but was of type {1}.",
                            typeof (T).Name,
                            result.GetType().Name)));
                }

                return Shared.Monads.Result<T, Exception>.CreateSuccess((T)result);
            }
            catch (Exception e)
            {
                return Shared.Monads.Result<T, Exception>.CreateError(e);
            }
            finally
            {
                source.Position = position;
            }
        }

        public static Shared.Monads.Result<object, Exception> TryDeserialize(this IDataSerializer serializer, Stream source, Type type)
        {
            var position = source.Position;
            try
            {
                var result = serializer.Deserialize(source, type);
                if (result == null)
                {
                    return Shared.Monads.Result<object, Exception>.CreateError(new SerializationException("Serializer returned null"));
                }

                var actualType = result.GetType();
                if (!type.IsAssignableFrom(actualType))
                {
                    return Shared.Monads.Result<object, Exception>.CreateError(new InvalidCastException(
                        String.Format("Source was expected to be of type {0} but was of type {1}.",
                            type.Name,
                            actualType.Name)));
                }

                return Shared.Monads.Result<object, Exception>.CreateSuccess(result);
            }
            catch (Exception e)
            {
                return Shared.Monads.Result<object, Exception>.CreateError(e);
            }
            finally
            {
                source.Position = position;
            }
        }

        public static Shared.Monads.Result<T, Exception> TryDeserializeAs<T>(this IDataSerializer serializer, byte[] source)
        {
            using (var stream = new MemoryStream(source))
            {
                return TryDeserializeAs<T>(serializer, stream);
            }
        }

        public static Shared.Monads.Result<XElement, Exception> TryUnpackXml(this IIntermediateDataSerializer serializer, Stream source)
        {
            var position = source.Position;
            try
            {
                var result = serializer.UnpackXml(source);
                if (result == null)
                {
                    return Shared.Monads.Result<XElement, Exception>.CreateError(new SerializationException("Serializer returned null"));
                }

                return Shared.Monads.Result<XElement, Exception>.CreateSuccess(result);
            }
            catch (Exception e)
            {
                return Shared.Monads.Result<XElement, Exception>.CreateError(e);
            }
            finally
            {
                source.Position = position;
            }
        }
    }
}
