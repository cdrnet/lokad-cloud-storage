#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Autofac;
using Autofac.Builder;
using Autofac.Configuration;
using Lokad.Cloud.Diagnostics;

namespace Lokad.Cloud.ServiceFabric.Runtime
{
	/// <summary>Organize the executions of the services.</summary>
	internal class Runtime
	{
		readonly CloudInfrastructureProviders _providers;
		readonly IServiceMonitor _monitoring;
		readonly DiagnosticsAcquisition _diagnostics;

		/// <summary>Main thread used to schedule services in <see cref="Execute()"/>.</summary>
		Thread _executeThread;

		volatile bool _isStopRequested;
		Scheduler _scheduler;

		/// <summary>Container used to populate cloud service properties.</summary>
		public IContainer RuntimeContainer { get; set; }

		/// <summary>IoC constructor.</summary>
		public Runtime(CloudInfrastructureProviders providers, ICloudDiagnosticsRepository diagnosticsRepository)
		{
			_providers = providers;
			_monitoring = new ServiceMonitor(diagnosticsRepository);
			_diagnostics = new DiagnosticsAcquisition(diagnosticsRepository);
		}

		/// <summary>Called once by the service fabric. Call is not supposed to return
		/// until stop is requested, or an uncaught exception is thrown.</summary>
		public void Execute()
		{
			_providers.Log.Log(LogLevel.Debug, string.Format("Runtime: started on worker {0}.", CloudEnvironment.PartitionKey));

			// hook on the current thread to force shut down
			_executeThread = Thread.CurrentThread;

			var clientContainer = RuntimeContainer;

			var loader = new AssemblyLoader(_providers.BlobStorage);
			loader.LoadPackage();

			// processing configuration file as retrieved from the blob storage.
			var config = loader.LoadConfiguration();
			if (config.HasValue)
			{
				ApplyConfiguration(config.Value, clientContainer);
			}

			// give the client a chance to register external diagnostics sources
			clientContainer.InjectProperties(_diagnostics);

			_scheduler = new Scheduler(() => LoadServices<CloudService>(clientContainer), RunService);

			try
			{
				foreach (var action in _scheduler.Schedule())
				{
					if (_isStopRequested)
					{
						break;
					}

					action();
				}
			}
			catch (ThreadInterruptedException)
			{
				_providers.Log.Log(LogLevel.Warn, string.Format(
					"Runtime: execution was interrupted on worker {0} in service {1}. The Runtime will be restarted.",
					CloudEnvironment.PartitionKey, GetNameOfServiceInExecution()));
			}
			catch (ThreadAbortException)
			{
				Thread.ResetAbort();

				_providers.Log.Log(LogLevel.Info, string.Format(
					"Runtime: execution was aborted on worker {0} in service {1}. The Runtime is stopping.",
					CloudEnvironment.PartitionKey, GetNameOfServiceInExecution()));
			}
			catch (TimeoutException)
			{
				_providers.Log.Log(LogLevel.Warn, string.Format(
					"Runtime: execution timed out on worker {0} in service {1}. The Runtime will be restarted.",
					CloudEnvironment.PartitionKey, GetNameOfServiceInExecution()));
			}
			catch (TriggerRestartException)
			{
				// Supposed to be handled by the runtime host (i.e. SingleRuntimeHost)
				throw;
			}
			catch (Exception ex)
			{
				_providers.Log.Log(LogLevel.Error, ex, string.Format(
					"Runtime: An unhandled {0} exception occurred on worker {1} in service {2}. The Runtime will be restarted.",
					ex.GetType().Name, CloudEnvironment.PartitionKey, GetNameOfServiceInExecution()));
			}
			finally
			{
				_providers.Log.Log(LogLevel.Debug, string.Format("Runtime: stopping on worker {0}.", CloudEnvironment.PartitionKey));

				_providers.RuntimeFinalizer.FinalizeRuntime();
				TryDumpDiagnostics();

				_providers.Log.Log(LogLevel.Debug, string.Format("Runtime: stopped on worker {0}.", CloudEnvironment.PartitionKey));
			}
		}

		/// <summary>The name of the service that is being executed, if any, <c>null</c> otherwise.</summary>
		private string GetNameOfServiceInExecution()
		{
			var scheduler = _scheduler;
			CloudService service;
			if (scheduler == null || (service = scheduler.CurrentlyScheduledService) == null)
			{
				return "unknown";
			}

			return service.Name;
		}

		/// <summary>Stops all services at once.</summary>
		/// <remarks>Called once by the service fabric when environment is about to
		/// be shut down.</remarks>
		public void Stop()
		{
			_isStopRequested = true;
			_providers.Log.Log(LogLevel.Debug, string.Format("Runtime: Stop() on worker {0}.", CloudEnvironment.PartitionKey));

			if (_executeThread != null)
			{
				_executeThread.Abort();
				return;
			}

			if (_scheduler != null)
			{
				_scheduler.AbortWaitingSchedule();
			}
		}

		/// <summary>
		/// Load and get all initialized service instances using the provided IoC container.
		/// </summary>
		static IEnumerable<T> LoadServices<T>(IContainer container)
		{
			var serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
				.Select(a => a.GetExportedTypes()).SelectMany(x => x)
				.Where(t => t.IsSubclassOf(typeof (T)) && !t.IsAbstract && !t.IsGenericType)
				.ToList();

			var builder = new ContainerBuilder();
			foreach (var type in serviceTypes)
			{
				builder.Register(type)
					.OnActivating((s, e) =>
						{
							e.Context.InjectUnsetProperties(e.Instance);

							var initializable = e.Instance as IInitializable;
							if (initializable != null)
							{
								initializable.Initialize();
							}
						})
					.FactoryScoped()
					.ExternallyOwned();

				// ExternallyOwned: to prevent the container from disposing the
				// cloud services - we manage their lifetime on our own using
				// e.g. RuntimeFinalizer
			}

			builder.Build(container);

			return serviceTypes.Select(type => (T) container.Resolve(type));
		}

		/// <summary>
		/// Run a scheduled service
		/// </summary>
		ServiceExecutionFeedback RunService(CloudService service)
		{
			ServiceExecutionFeedback feedback;

			using (_monitoring.Monitor(service))
			{
				feedback = service.Start();
			}

			return feedback;
		}

		/// <summary>
		/// Try to dump diagnostics, but suppress any exceptions if it fails
		/// </summary>
		void TryDumpDiagnostics()
		{
			try
			{
				_diagnostics.CollectStatistics();
			}
			catch (ThreadAbortException)
			{
				Thread.ResetAbort();
				_providers.Log.WarnFormat("Runtime: skipped acquiring statistics on worker {0}", CloudEnvironment.PartitionKey);
				// TODO: consider 2nd trial here
			}
			// ReSharper disable EmptyGeneralCatchClause
			catch(Exception e)
			{
				_providers.Log.ErrorFormat(e, "Runtime: failed to acquire statistics on worker {0}: {1}", CloudEnvironment.PartitionKey, e.Message);
				// might fail when shutting down on exception
				// logging is likely to fail as well in this case
				// Suppress exception, can't do anything (will be recycled anyway)
			}
			// ReSharper restore EmptyGeneralCatchClause
		}

		/// <summary>
		/// Apply the configuration provided in text as raw bytes to the provided IoC
		/// container.
		/// </summary>
		static void ApplyConfiguration(byte[] config, IContainer container)
		{
			// HACK: need to copy settings locally first
			// HACK: hard-code string for local storage name
			const string fileName = "lokad.cloud.clientapp.config";
			const string resourceName = "LokadCloudStorage";

			var pathToFile = Path.Combine(
				CloudEnvironment.GetLocalStoragePath(resourceName),
				fileName);

			File.WriteAllBytes(pathToFile, config);
			var configReader = new ConfigurationSettingsReader("autofac", pathToFile);

			configReader.Configure(container);
		}
	}
}