#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lokad.Cloud.Runtime;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Storage;
using Lokad.Cloud.ServiceFabric.Runtime;
using Mono.Cecil;

namespace Lokad.Cloud.Application
{
    /// <summary>
    /// Utility to inspect a cloud application package and its services.
    /// </summary>
    /// <remarks>
    /// Mono.Cecil is used instead of .NET Reflector:
    /// 1. So we don't need to use an AppDomain to be able to unload the assemblies afterwards.
    /// 2. So we can reflect without resolving and loading any dependent assemblies (AutoFac, Lookad.Cloud.Storage)
    /// </remarks>
    public class CloudApplicationInspector
    {
        public const string ContainerName = "lokad-cloud-assemblies";
        public const string ApplicationDefinitionBlobName = "definition";

        private readonly IBlobStorageProvider _blobs;

        public CloudApplicationInspector(RuntimeProviders runtimeProviders)
        {
            _blobs = runtimeProviders.BlobStorage;
        }

        public Maybe<CloudApplicationDefinition> Inspect()
        {
            var definitionBlob = _blobs.GetBlob<CloudApplicationDefinition>(ContainerName, ApplicationDefinitionBlobName);
            Maybe<byte[]> packageBlob;
            string packageETag;

            if (definitionBlob.HasValue)
            {
                packageBlob = _blobs.GetBlobIfModified<byte[]>(AssemblyLoader.ContainerName, AssemblyLoader.PackageBlobName, definitionBlob.Value.PackageETag, out packageETag);
                if (!packageBlob.HasValue || definitionBlob.Value.PackageETag == packageETag)
                {
                    return definitionBlob.Value;
                }
            }
            else
            {
                packageBlob = _blobs.GetBlob<byte[]>(AssemblyLoader.ContainerName, AssemblyLoader.PackageBlobName, out packageETag);
            }

            if (!packageBlob.HasValue)
            {
                return Maybe<CloudApplicationDefinition>.Empty;
            }

            var definition = Analyze(packageBlob.Value, packageETag);
            _blobs.PutBlob(ContainerName, ApplicationDefinitionBlobName, definition);
            return definition;
        }

        private static CloudApplicationDefinition Analyze(byte[] packageData, string etag)
        {
            var reader = new CloudApplicationPackageReader();
            var package = reader.ReadPackage(packageData, true);

            var cloudServiceTypeDefinitions = new List<TypeDefinition>();
            var queueServiceTypeDefinitions = new List<TypeDefinition>();
            var scheduledServiceTypeDefinitions = new List<TypeDefinition>();

            var typeDefinitionMaps = new Dictionary<string, TypeDefinition>();
            var serviceBaseTypes = new Dictionary<string, List<TypeDefinition>>
                {
                    { typeof(CloudService).FullName, cloudServiceTypeDefinitions },
                    { typeof(ScheduledService).FullName, scheduledServiceTypeDefinitions },
                    { typeof(QueueService<>).FullName, queueServiceTypeDefinitions }
                };

            // Instead of resolving, we reflect iteratively with multiple passes.
            // This way we can avoid loading referenced assemblies like AutoFac
            // and Lokad.Cloud.Storage which may have mismatching versions
            // (e.g. Autofac 2 is completely incompatible to Autofac 1)

            var assebliesBytes = package.Assemblies.Select(package.GetAssembly).ToList();
            assebliesBytes.Add(File.ReadAllBytes(typeof(CloudService).Assembly.Location));

            bool newTypesFoundThisRound;
            do
            {
                newTypesFoundThisRound = false;
                foreach (var assemblyBytes in assebliesBytes)
                {
                    using (var stream = new MemoryStream(assemblyBytes))
                    {
                        var definition = AssemblyDefinition.ReadAssembly(stream);
                        foreach (var typeDef in definition.MainModule.Types)
                        {
                            if (typeDef.BaseType == null || typeDef.BaseType.FullName == "System.Object" || serviceBaseTypes.ContainsKey(typeDef.FullName))
                            {
                                continue;
                            }

                            var baseTypeName = typeDef.BaseType.IsGenericInstance
                                ? typeDef.BaseType.Namespace + "." + typeDef.BaseType.Name
                                : typeDef.BaseType.FullName;

                            List<TypeDefinition> matchingServiceTypes;
                            if (!serviceBaseTypes.TryGetValue(baseTypeName, out matchingServiceTypes))
                            {
                                continue;
                            }

                            typeDefinitionMaps.Add(typeDef.FullName, typeDef);
                            serviceBaseTypes.Add(typeDef.FullName, matchingServiceTypes);
                            newTypesFoundThisRound = true;

                            if (!typeDef.IsAbstract && !typeDef.HasGenericParameters)
                            {
                                matchingServiceTypes.Add(typeDef);
                            }
                        }
                    }
                }
            }
            while (newTypesFoundThisRound);

            return new CloudApplicationDefinition
                {
                    PackageETag = etag,
                    Timestamp = DateTimeOffset.UtcNow,
                    Assemblies = package.Assemblies.ToArray(),
                    ScheduledServices = scheduledServiceTypeDefinitions.Select(td => new ScheduledServiceDefinition { TypeName = td.FullName }).ToArray(),
                    CloudServices = cloudServiceTypeDefinitions.Select(td => new CloudServiceDefinition { TypeName = td.FullName }).ToArray(),
                    QueueServices = queueServiceTypeDefinitions.Select(td =>
                        {
                            var messageType = GetQueueServiceMessageType(td, typeDefinitionMaps);
                            return new QueueServiceDefinition
                            {
                                TypeName = td.FullName,
                                MessageTypeName = messageType.FullName,
                                QueueName = GetAttributeProperty(td, typeof(QueueServiceSettingsAttribute).FullName, "QueueName", () => messageType.FullName.ToLowerInvariant().Replace(".", "-"))
                            };
                        }).ToArray(),
                };
        }

        private static T GetAttributeProperty<T>(TypeDefinition type, string attributeName, string propertyName, Func<T> defaultValue)
        {
            var attribute = type.CustomAttributes.FirstOrDefault(ca => ca.AttributeType.FullName == attributeName);
            if (attribute == null)
            {
                return defaultValue();
            }

            var property = attribute.Properties.FirstOrDefault(p => p.Name == propertyName);
            if (property.Name == null)
            {
                return defaultValue();
            }

            return (T)property.Argument.Value;
        }

        private static TypeReference GetQueueServiceMessageType(TypeDefinition typeDefinition, Dictionary<string, TypeDefinition> typeDefinitionMaps)
        {
            var baseRef = typeDefinition.BaseType;
            var baseRefName = baseRef.Namespace + "." + baseRef.Name;
            if (baseRefName == typeof(QueueService<>).FullName)
            {
                return ((GenericInstanceType)baseRef).GenericArguments[0];
            }

            var parentMessageType = GetQueueServiceMessageType(typeDefinitionMaps[baseRefName], typeDefinitionMaps);
            if (!parentMessageType.IsGenericParameter)
            {
                return parentMessageType;
            }

            return ((GenericInstanceType)baseRef).GenericArguments[0];
        }
    }
}
