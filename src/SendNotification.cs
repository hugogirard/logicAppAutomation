using System.Net;
using Contoso.Models;
using Contoso.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Contoso.Application;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace Contoso;

public class SendNotification(ILogger<SendNotification> _logger, IGraphService _graphService, IMonitoringService _monitoringService)
{
    [Function("GetVms")]
    [OpenApiOperation(operationId: "GetVms")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<VmState>),
            Description = "The OK response message containing the list of Vms.")]
    public async Task<IActionResult> GetVms([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {
        _logger.LogInformation("Trigger GetVms");

        var vms = await _graphService.RetrieveVmsPending();

        _logger.LogInformation($"Find {vms.Count()} in pending state");

        if (vms.Count() == 0)
        {
            return new OkObjectResult("No vms are pending reboot state");
        }

        // Get only VM in pending state
        // and extract the resource ID
        var pendingStates = vms.Where(x => x.RestartPending)
                               .Select(x => x.ResourceId);

        return new OkObjectResult(vms);
    }

    [Function("GetVmsRebootInfo")]
    [OpenApiOperation(operationId: "GetVmsRebootInfo")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<VmInPendingState>),
            Description = "The OK response message containing the list of Vms.")]
    public async Task<IActionResult> GetVmsRebootInfo([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {

        var vms = await _graphService.RetrieveVmsPending();

        var pendingStates = vms.Where(x => x.RestartPending)
                               .Select(x => x.ResourceId);

        var vmsInfo = await _graphService.GetOwnerInfoVms(pendingStates);

        return new OkObjectResult(vmsInfo);
    }

    [Function("SendEmail")]
    [OpenApiOperation(operationId: "SendEmail")]
    [OpenApiRequestBody("application/json", typeof(EmailRequest))]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]

    public async Task<IActionResult> SendEmail([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
                                                                                                  [FromBody] EmailRequest emailRequest)
    {
        try
        {
            await _graphService.SendEmail(emailRequest.message, emailRequest.from, emailRequest.to);

            return new OkResult();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    [Function("SendEmailVmsPendingStateNotification")]
    [OpenApiOperation(operationId: "SendEmailVmsPendingStateNotification")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    public async Task<IActionResult> SendEmailVmsPendingStateNotification([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        try
        {
            await _monitoringService.SendEmailsVmPendingState();
            return new OkResult();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

}