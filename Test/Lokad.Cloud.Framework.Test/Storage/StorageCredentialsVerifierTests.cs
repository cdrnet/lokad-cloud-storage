#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage;
using NUnit.Framework;

namespace Lokad.Cloud.Test.Storage
{
    [TestFixture]
    public class StorageCredentialsVerifierTests
    {
        [Test]
        public void VerifyCredentials()
        {
            var verifier = new StorageCredentialsVerifier(GlobalSetup.Container);

            Assert.IsTrue(verifier.VerifyCredentials(), "Credentials should be verified");
        }
    }
}
