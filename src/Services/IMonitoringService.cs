namespace Contoso.Services;

public interface IMonitoringService
{
    Task SendEmailsVmPendingState();
}
