#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Storage.Autofac
{
    /// <summary>
    /// Storage for logging that do not log themselves (breaking potential cycles)
    /// </summary>
    public class NeutralLogStorage
    {
        public IBlobStorageProvider BlobStorage { get; set; }
    }
}
