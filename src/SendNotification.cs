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

namespace Contoso;

public class SendNotification
{
    private readonly ILogger<SendNotification> _logger;
    private readonly IGraphService _graphService;

    public SendNotification(ILogger<SendNotification> logger, IGraphService graphService)
    {
        _logger = logger;
        _graphService = graphService;
    }

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
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string),
            Description = "The OK response message containing the list of Vms.")]
    public async Task<IActionResult> GetVmsRebootInfo([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {

        var vms = await _graphService.RetrieveVmsPending();

        var pendingStates = vms.Where(x => x.RestartPending)
                               .Select(x => x.ResourceId);

        var vmsInfo = await _graphService.GetOwnerInfoVms(pendingStates);

        return new OkObjectResult(vmsInfo);
    }


}