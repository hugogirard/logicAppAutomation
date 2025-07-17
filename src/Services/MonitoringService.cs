using Microsoft.Extensions.Logging;

namespace Contoso.Services;

public class MonitoringService(IGraphService _graphService,
                               ILogger<MonitoringService> _logger) : IMonitoringService
{
    public async Task SendEmailsVmPendingState()
    {
        var vmsPending = await _graphService.RetrieveVmsPending();

        var pendingStates = vmsPending.Where(x => x.RestartPending)
                                      .Select(x => x.ResourceId);

        if (pendingStates.Count() == 0)
        {
            _logger.LogInformation("No vms in pending state");
            return;
        }

        _logger.LogInformation($"Found {pendingStates.Count()} in pending state");

        var vmsInfo = await _graphService.GetOwnerInfoVms(pendingStates);

        _logger.LogInformation($"Found infos for {vmsInfo.Count()} vms");

        if (vmsInfo.Count() == 0)
            return;

        foreach (var vmInfo in vmsInfo)
        {
            _logger.LogInformation($"Sending email to {vmInfo.Owner} - {vmInfo.Contact} for vms {vmInfo.Name}");

            await _graphService.SendEmailVmPendingState(vmInfo);
            await Task.Delay(1000); // Safe async sleep for 1 second
        }

    }
}