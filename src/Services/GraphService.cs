using Azure.Core;
using Azure.Identity;
using Contoso.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Contoso.Services;

public class GraphService : IGraphService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _workspaceId;
    private readonly string? _getVmsQuery;
    private readonly ILogger<GraphService> _logger;
    private readonly string _from;
    private readonly GraphServiceClient _emailClient;

    public GraphService(IHttpClientFactory factory, IConfiguration configuration, ILogger<GraphService> logger)
    {
        _httpClientFactory = factory;
        _workspaceId = configuration["workspaceID"] ?? throw new ArgumentException("The workspaceID settings cannot be null");
        _getVmsQuery = configuration["GetVMQuery"] ?? throw new ArgumentException("The GetVMQuery settings cannot be null");
        _logger = logger;
        _from = configuration["SenderEmail"] ?? throw new ArgumentException("The SenderEmail settings cannot be null");

        var tenantId = configuration["TenantID"] ?? throw new ArgumentException("The TenantID settings cannot be null");
        var clientId = configuration["ClientID"] ?? throw new ArgumentException("The ClientID settings cannot be null");
        var secret = configuration["ClientSecret"] ?? throw new ArgumentException("The ClientSecret settings cannot be null");

        var scopes = new[] { "https://graph.microsoft.com/.default" };
        var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, secret);
        _emailClient = new GraphServiceClient(clientSecretCredential, scopes);
    }

    public async Task<IEnumerable<VmInPendingState>> GetOwnerInfoVms(IEnumerable<string> resourceIds)
    {
        var query = new StringBuilder();

        query.AppendLine("arg('').resources | where type contains 'microsoft.compute/virtualmachines'");
        int idx = 0;
        foreach (var id in resourceIds)
        {
            if (idx == 0)
                query.AppendLine($"| where tolower(id) == tolower('{id}')");
            else
                query.Append($" or tolower(id) == tolower('{id}')");

            idx++;
        }

        query.AppendLine("| project id, name, subscriptionId, resourceGroup, owner = tostring(tags['owner']), contact = tostring(tags['contact'])");

        _logger.LogInformation($"Query to retrieve Vms info {query.ToString()}");

        var result = await ExecuteQuery<KustoResult>(query.ToString());

        if (result is null)
        {
            _logger.LogInformation("No vms info found");
            return new List<VmInPendingState>();
        }


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

    public async Task SendEmail(string message, string from, string to)
    {
        try
        {
            var email = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = new Microsoft.Graph.Models.Message
                {
                    Subject = "Test email",
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Text,
                        Content = message
                    },
                    ToRecipients = new List<Microsoft.Graph.Models.Recipient>
            {
                new Microsoft.Graph.Models.Recipient
                {
                    EmailAddress = new Microsoft.Graph.Models.EmailAddress
                    {
                        Address = to
                    }
                }
            }
                }
            };

            await _emailClient.Users[from].SendMail.PostAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message, ex);
        }
    }

    public async Task SendEmailVmPendingState(VmInPendingState vm)
    {
        try
        {
            var email = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = new Microsoft.Graph.Models.Message
                {
                    Subject = $"VM {vm.Name} needs to be rebooted",
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Text,
                        Content = $"Hi {vm.Owner}, the VM '{vm.Name}' is in a pending state. SubscriptionID: {vm.SubscriptionId} - ResourceGroup: {vm.ResourceGroup}"
                    },
                    ToRecipients = new List<Microsoft.Graph.Models.Recipient>
            {
                new Microsoft.Graph.Models.Recipient
                {
                    EmailAddress = new Microsoft.Graph.Models.EmailAddress
                    {
                        Address = vm.Contact
                    }
                }
            }
                }
            };

            await _emailClient.Users[_from].SendMail.PostAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }
    }

    private async Task<T?> ExecuteQuery<T>(string query) where T : class
    {
        Azure.Core.AccessToken token = await GetBearerToken("https://api.loganalytics.io/.default");

        string url = $"https://api.loganalytics.io/v1/workspaces/{_workspaceId}/query?query={query}";

        var http = _httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        List<T> vms = new();
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

    private async Task<AccessToken> GetBearerToken(string scope)
    {
        var credential = new DefaultAzureCredential();
        var tokenRequestContext = new TokenRequestContext(new[] { scope });
        var token = await credential.GetTokenAsync(tokenRequestContext, default);
        return token;
    }
}