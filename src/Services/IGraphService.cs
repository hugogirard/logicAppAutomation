using Contoso.Models;
namespace Contoso.Services;

public interface IGraphService
{
    Task<List<VmState>> RetrieveVmsPending();

    Task<IEnumerable<VmInPendingState>> GetOwnerInfoVms(IEnumerable<string> resourceIds);

    Task SendEmailVmPendingState(VmInPendingState vm);

    Task SendEmail(string message, string from, string to);
}
