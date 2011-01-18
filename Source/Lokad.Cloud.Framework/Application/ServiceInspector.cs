namespace Lokad.Cloud.Application
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Lokad.Cloud.ServiceFabric;

    internal class ServiceInspector : IDisposable
    {
        bool _disposed;
        readonly AppDomain _sandbox;
        readonly Wrapper _wrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AssemblyVersionInspector"/> class.
        /// </summary>
        /// <param name="assemblyBytes">The assembly bytes.</param>
        /// <param name="symbolBytes">The symbol store bytes if available, else null.</param>
        public ServiceInspector(byte[] packageData)
        {
            _sandbox = AppDomain.CreateDomain("ServiceInspector", null, AppDomain.CurrentDomain.SetupInformation);
            _wrapper = _sandbox.CreateInstanceAndUnwrap(
                Assembly.GetExecutingAssembly().FullName,
                (typeof (Wrapper)).FullName,
                false,
                BindingFlags.CreateInstance,
                null,
                new object[] { packageData },
                null,
                new object[0]) as Wrapper;
        }

        public List<QueueServiceDefinition> QueueServices
        {
            get { return _wrapper.QueueServices; }
        }

        public List<ScheduledServiceDefinition> ScheduledServices
        {
            get { return _wrapper.ScheduledServices; }
        }

        public List<CloudServiceDefinition> CloudServices
        {
            get { return _wrapper.CloudServices; }
        }

        /// <summary>Disposes of the object and the wrapped <see cref="AppDomain"/>.</summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                AppDomain.Unload(_sandbox);
                _disposed = true;
            }
        }

        /// <summary>
        /// Wraps an assembly (to be used from within a secondary AppDomain).
        /// </summary>
        public class Wrapper : MarshalByRefObject
        {
            readonly Assembly _wrappedAssembly;
            public List<QueueServiceDefinition> QueueServices;
            public List<ScheduledServiceDefinition> ScheduledServices;
            public List<CloudServiceDefinition> CloudServices;

            /// <summary>
            /// Initializes a new instance of the <see cref="T:Wrapper"/> class.
            /// </summary>
            /// <param name="assemblyBytes">The assembly bytes.</param>
            public Wrapper(byte[] packageData)
            {
                var reader = new CloudApplicationPackageReader();
                var package = reader.ReadPackage(packageData, false);
                package.LoadAssemblies();

                var serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetExportedTypes()).SelectMany(x => x)
                    .Where(t => t.IsSubclassOf(typeof(CloudService)) && !t.IsAbstract && !t.IsGenericType)
                    .ToList();

                var scheduledServiceTypes = serviceTypes.Where(t => t.IsSubclassOf(typeof(ScheduledService))).ToList();
                var queueServiceTypes = serviceTypes.Where(t => IsSubclassOfRawGeneric(t, typeof(QueueService<>))).ToList();
                var cloudServiceTypes = serviceTypes.Except(scheduledServiceTypes).Except(queueServiceTypes).ToList();

                this.ScheduledServices = scheduledServiceTypes
                    .Select(t => new ScheduledServiceDefinition { TypeName = t.FullName })
                    .ToList();

                this.CloudServices = cloudServiceTypes
                    .Select(t => new CloudServiceDefinition { TypeName = t.FullName })
                    .ToList();

                this.QueueServices = queueServiceTypes
                    .Select(t =>
                        {
                            var messageType = GetBaseClassGenericTypeParameters(t, typeof(QueueService<>))[0];

                            var attribute = t.GetAttribute<QueueServiceSettingsAttribute>(true);
                            var queueName = (attribute != null && !String.IsNullOrEmpty(attribute.QueueName))
                                ? attribute.QueueName
                                : TypeMapper.GetStorageName(messageType);

                            return new QueueServiceDefinition
                                {
                                    TypeName = t.FullName,
                                    MessageTypeName = messageType.FullName,
                                    QueueName = queueName
                                };
                        })
                    .ToList();
            }

            static bool IsSubclassOfRawGeneric(Type type, Type baseGenericTypeDefinition)
            {
                while (type != typeof(object))
                {
                    var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                    if (baseGenericTypeDefinition == cur)
                    {
                        return true;
                    }
                    type = type.BaseType;
                }
                return false;
            }

            static Type[] GetBaseClassGenericTypeParameters(Type type, Type baseGenericTypeDefinition)
            {
                while (type != typeof(object))
                {
                    var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                    if (baseGenericTypeDefinition == cur)
                    {
                        return type.GetGenericArguments();
                    }
                    type = type.BaseType;
                }
                
                throw new InvalidOperationException();
            }
        }
    }
}
