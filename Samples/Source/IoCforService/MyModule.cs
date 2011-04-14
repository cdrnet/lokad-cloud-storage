#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Autofac.Builder;

namespace IoCforService
{
	public class MyModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
            builder.Register(c => new MyProvider(c.Resolve<Lokad.Cloud.Storage.Shared.Logging.ILog>()));
		}
	}
}
