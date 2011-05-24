using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Management;

namespace Lokad.Cloud.Mock
{
    public class MemoryProvisioning : IProvisioningProvider
    {
        public Task SetWorkerInstanceCount(int count, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() => { });
        }

        public Task<int> GetWorkerInstanceCount(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() => 1);
        }
    }
}
