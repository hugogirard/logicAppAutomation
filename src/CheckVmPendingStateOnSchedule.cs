using Contoso.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;

namespace Contoso;

public class CheckVmPendingStateOnSchedule
{
    private readonly ILogger _logger;
    private readonly IMonitoringService _monitoringService;

    public CheckVmPendingStateOnSchedule(ILoggerFactory loggerFactory, IMonitoringService monitoringService)
    {
        _logger = loggerFactory.CreateLogger<CheckVmPendingStateOnSchedule>();
        _monitoringService = monitoringService;
    }

    [Function("CheckVmPendingStateOnSchedule")]
    public async Task Run([TimerTrigger("%CheckVmPendingStateOnScheduleCron%")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }

        try
        {
            await _monitoringService.SendEmailsVmPendingState();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }
    }
}