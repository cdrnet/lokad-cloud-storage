#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using Lokad;

namespace IoCforService
{
	/// <summary>Sample provider, registered though Autofac module.</summary>
	public class MyProvider
	{
        public MyProvider(Lokad.Cloud.Storage.Shared.Logging.ILog logger)
		{
            logger.Log(Lokad.Cloud.Storage.Shared.Logging.LogLevel.Info, "Client IoC module loaded.");
		}
	}
}
