#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lokad.Cloud.ServiceFabric;

namespace Lokad.Cloud.Application
{
    /// <summary>
    /// Utility to inspect cloud services in an isolated AppDomain.
    /// </summary>
    internal static class ServiceInspector
    {
        internal static ServiceInspectionResult Inspect(byte[] packageBytes)
        {
            var sandbox = AppDomain.CreateDomain("ServiceInspector", null, AppDomain.CurrentDomain.SetupInformation);
            try
            {
                var wrapper = (Wrapper)sandbox.CreateInstanceAndUnwrap(
                    Assembly.GetExecutingAssembly().FullName,
                    (typeof(Wrapper)).FullName,
                    false,
                    BindingFlags.CreateInstance,
                    null,
                    new object[] { packageBytes },
                    null,
                    new object[0]);

                return wrapper.Result;
            }
            finally
            {
                AppDomain.Unload(sandbox);
            }
        }

        [Serializable]
        internal class ServiceInspectionResult
        {
            public List<QueueServiceDefinition> QueueServices { get; set; }
            public List<ScheduledServiceDefinition> ScheduledServices { get; set; }
            public List<CloudServiceDefinition> CloudServices { get; set; }
        }

        /// <summary>
        /// Wraps an assembly (to be used from within a secondary AppDomain).
        /// </summary>
        private class Wrapper : MarshalByRefObject
        {
            internal ServiceInspectionResult Result { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
            /// <param name="packageBytes">The application package bytes.</param>
            public Wrapper(byte[] packageBytes)
            {
                var reader = new CloudApplicationPackageReader();
                var package = reader.ReadPackage(packageBytes, false);
                package.LoadAssemblies();

                var serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetExportedTypes()).SelectMany(x => x)
                    .Where(t => t.IsSubclassOf(typeof(CloudService)) && !t.IsAbstract && !t.IsGenericType)
                    .ToList();

                var scheduledServiceTypes = serviceTypes.Where(t => t.IsSubclassOf(typeof(ScheduledService))).ToList();
                var queueServiceTypes = serviceTypes.Where(t => IsSubclassOfRawGeneric(t, typeof(QueueService<>))).ToList();
                var cloudServiceTypes = serviceTypes.Except(scheduledServiceTypes).Except(queueServiceTypes).ToList();

                Result = new ServiceInspectionResult
                    {
                        ScheduledServices = scheduledServiceTypes
                            .Select(t => new ScheduledServiceDefinition { TypeName = t.FullName }).ToList(),
                        CloudServices = cloudServiceTypes
                            .Select(t => new CloudServiceDefinition { TypeName = t.FullName }).ToList(),
                        QueueServices = queueServiceTypes
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
                                }).ToList()
                    };
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
