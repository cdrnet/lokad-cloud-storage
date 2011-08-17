#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test.Blobs
{
    [TestFixture]
    [Category("DevelopmentStorage")]
    public class DevBlobStorageTests : BlobStorageTests
    {
        public DevBlobStorageTests()
            : base(CloudStorage.ForDevelopmentStorage().BuildStorageProviders())
        {
        }
    }
}
