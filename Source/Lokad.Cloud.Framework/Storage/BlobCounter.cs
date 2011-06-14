#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Storage
{
    /// <summary>Simple non-sharded counter shared among several workers.</summary>
    /// <remarks>The content of the counter is stored in a single blob value. Present design 
    /// starts to be slow when about 50 workers are trying to modify the same counter.
    /// Caution : this counter is not idempotent, so using it in services could lead to incorrect behaviour.</remarks>
    public class BlobCounter
    {
        readonly IBlobStorageProvider _provider;

        readonly string _containerName;
        readonly string _blobName;

        /// <summary>Constant value provided for the cloud enumeration pattern
        /// over a queue.</summary>
        /// <remarks>The constant value is <c>2^48</c>, expected to be sufficiently
        /// large to avoid any arithmetic overflow with <c>long</c> values.</remarks>
        public const long Aleph = 1L << 48;

        /// <summary>Container that is storing the counter.</summary>
        public string ContainerName { get { return _containerName; } }

        /// <summary>Blob that is storing the counter.</summary>
        public string BlobName { get { return _blobName; } }

        /// <summary>Shorthand constructor.</summary>
        public BlobCounter(IBlobStorageProvider provider, BlobName<decimal> fullName)
            : this(provider, fullName.ContainerName, fullName.ToString())
        {
        }

        /// <summary>Full constructor.</summary>
        public BlobCounter(IBlobStorageProvider provider, string containerName, string blobName)
        {
            if(null == provider) throw new ArgumentNullException("provider");
            if(null == containerName) throw new ArgumentNullException("containerName");
            if(null == blobName) throw new ArgumentNullException("blobName");

            _provider = provider;
            _containerName = containerName;
            _blobName = blobName;
        }

        /// <summary>Returns the value of the counter (or zero if there is no value to
        /// be returned).</summary>
        public decimal GetValue()
        {
            var value = _provider.GetBlob<decimal>(_containerName, _blobName);
            return value.HasValue ? value.Value : 0m;
        }

        /// <summary>Atomic increment the counter value.</summary>
        /// <remarks>If the counter does not exist before hand, it gets created with the provided increment value.</remarks>
        public decimal Increment(decimal increment)
        {
            return _provider.UpsertBlob(_containerName, _blobName, () => increment, x => x + increment);
        }

        /// <summary>Reset the counter at the given value.</summary>
        public void Reset(decimal value)
        {
            _provider.PutBlob(_containerName, _blobName, value);
        }

        /// <summary>Deletes the counter.</summary>
        /// <returns><c>true</c> if the counter has actually been deleted by the call,
        /// and <c>false</c> otherwise.</returns>
        public bool Delete()
        {
            return _provider.DeleteBlobIfExist(_containerName, _blobName);
        }
    }
}