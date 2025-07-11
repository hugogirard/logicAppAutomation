using Microsoft.Extensions.Configuration;
using Azure.Identity;
using System.Net.Http.Json;
using Contoso.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace Contoso.Services;

public class GraphService : IGraphService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _workspaceId;
    private readonly string? _getVmsQuery;
    private readonly ILogger<GraphService> _logger;

    public GraphService(IHttpClientFactory factory, IConfiguration configuration, ILogger<GraphService> logger)
    {
        _httpClientFactory = factory;
        _workspaceId = configuration["workspaceID"] ?? throw new ArgumentException("The workspaceID settings cannot be null");
        _getVmsQuery = configuration["GetVMQuery"] ?? throw new ArgumentException("The GetVMQuery settings cannot be null");
        _logger = logger;
    }

    public async Task<IEnumerable<VmInPendingState>> GetOwnerInfoVms(IEnumerable<string> resourceIds)
    {
        var query = new StringBuilder();

        query.AppendLine("arg('').resources | where type contains 'microsoft.compute/virtualmachines'");
        int idx = 0;
        foreach (var id in resourceIds)
        {
            if (idx == 0)
                query.AppendLine($"| where id == '{id}'");
            else
                query.Append($" or id == '{id}'");

            idx++;
        }

        query.AppendLine("| project id, name, subscriptionId, resourceGroup, owner = tostring(tags['owner']), contact = tostring(tags['contact'])");

        var result = await ExecuteQuery<KustoResult>(query.ToString());

        if (result is null)
            return new List<VmInPendingState>();

        return result.GetPendingVmsInfo();

    }

    public async Task<List<VmState>> RetrieveVmsPending()
    {

        List<VmState> vms = new();
        try
        {

#pragma warning disable CS8604 // Possible null reference argument.
            var result = await ExecuteQuery<KustoResult>(_getVmsQuery);
#pragma warning restore CS8604 // Possible null reference argument.

            if (result is null)
                return vms;

            return result.GetVmStateResults();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }
        return vms;

    }

    private async Task<T?> ExecuteQuery<T>(string query) where T : class
    {
        var credential = new DefaultAzureCredential();
        var tokenRequestContext = new Azure.Core.TokenRequestContext(new[] { "https://api.loganalytics.io/.default" });
        var token = await credential.GetTokenAsync(tokenRequestContext, default);

        string url = $"https://api.loganalytics.io/v1/workspaces/{_workspaceId}/query?query={query}";

        var http = _httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        List<VmState> vms = new();
        try
        {
            return await http.GetFromJsonAsync<T>(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }
        return default;
    }
}