#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test.Tables
{
    /// <remarks>Includes all unit tests for the real table provider</remarks>
    [TestFixture]
    [Category("DevelopmentStorage")]
    public class DevTableStorageTests : TableStorageTests
    {
        public DevTableStorageTests()
            : base(CloudStorage.ForDevelopmentStorage().BuildStorageProviders())
        {
        }
    }
}
