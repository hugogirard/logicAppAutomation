using Contoso.Models;
namespace Contoso.Services;

public interface IGraphService
{
    Task<List<VmState>> RetrieveVmsPending();

    Task<IEnumerable<VmInPendingState>> GetOwnerInfoVms(IEnumerable<string> resourceIds);
}
