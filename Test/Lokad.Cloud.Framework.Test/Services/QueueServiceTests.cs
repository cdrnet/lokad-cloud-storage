#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using Lokad.Cloud.Mock;
using Lokad.Cloud.Runtime;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.Shared.Logging;
using NUnit.Framework;

namespace Lokad.Cloud.Test.Services
{
    [TestFixture]
    public class QueueServiceTests
    {
        [Test]
        public void SquareServiceTest()
        {
            var storage = CloudStorage.ForInMemoryStorage().BuildStorageProviders();
            var runtimeStorage = CloudStorage.ForInMemoryStorage().BuildRuntimeProviders();
            var legacyProviders = new CloudInfrastructureProviders(storage, new MemoryProvisioning(), NullLog.Instance);
            
            var service = new SquareQueueService { Providers = legacyProviders, RuntimeProviders = runtimeStorage };

            const string containerName = "mockcontainer";

            //filling blobs to be processed.
            for (int i = 0; i < 10; i++)
            {
                storage.BlobStorage.PutBlob(containerName, "blob" + i, (double) i);
            }

            var squareMessage = new SquareMessage
                {
                    ContainerName = containerName,
                    Expiration = DateTimeOffset.UtcNow + new TimeSpan(10, 0, 0, 0),
                    IsStart = true
                };

            var queueName = TypeMapper.GetStorageName(typeof(SquareMessage));
            storage.QueueStorage.Put(queueName, squareMessage);

            for (int i = 0 ; i < 11 ; i++)
            {
                service.StartService();
            }

            var sum = storage.BlobStorage.ListBlobs<double>(containerName).Sum();
 
            //0*0+1*1+2*2+3*3+...+9*9 = 285
            Assert.AreEqual(285, sum, "result is different from expected.");	
        }

        [Serializable]
        class SquareMessage
        {
            public bool IsStart { get; set; }

            public DateTimeOffset Expiration { get; set; }

            public string ContainerName { get; set;}

            public string BlobName { get; set;}

            public TemporaryBlobName<decimal> BlobCounter { get; set; }
        }

        [QueueServiceSettings(AutoStart = true, //QueueName = "SquareQueue",
        Description = "multiply numbers by themselves.")]
        class SquareQueueService : QueueService<SquareMessage>
        {
            public void StartService()
            {
                StartImpl();
            }

            protected override void Start(SquareMessage message)
            {
                var blobStorage = Providers.BlobStorage;

                if (message.IsStart)
                {
                    var counterName = TemporaryBlobName<decimal>.GetNew(message.Expiration);
                    var counter = new BlobCounter(blobStorage, counterName);
                    counter.Reset(BlobCounter.Aleph);

                    var blobNames = blobStorage.ListBlobNames(message.ContainerName).ToList();
                    
                    foreach (var blobName in blobNames)
                    {
                        Put(new SquareMessage
                            {
                                BlobName = blobName,
                                ContainerName = message.ContainerName,
                                IsStart = false,
                                BlobCounter = counterName
                            });
                    }

                    // dealing with rare race condition
                    if (0m >= counter.Increment(-BlobCounter.Aleph + blobNames.Count))
                    {
                        Finish(counter);
                    }
                    
                }
                else
                {
                    var value = blobStorage.GetBlob<double>(message.ContainerName, message.BlobName).Value;
                    blobStorage.PutBlob(message.ContainerName, message.BlobName, value * value);

                    var counter = new BlobCounter(Providers.BlobStorage, message.BlobCounter);
                    if (0m >= counter.Increment(-1))
                    {
                        Finish(counter);
                    }
                }
            }

            void Finish(BlobCounter counter)
            {
                counter.Delete();
            }
        }
    }
}
