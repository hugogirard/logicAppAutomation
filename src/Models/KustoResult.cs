using System.Text.Json.Serialization;

namespace Contoso.Models;

public class KustoResult
{
    [JsonPropertyName("tables")]
    public List<KustoTable> Tables { get; set; } = new();
}

public class KustoTable
{
    [JsonPropertyName("rows")]
    public List<List<object>> Rows { get; set; } = new();
}

public class VmInPendingState
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
}

// Strongly-typed model for the specific data structure in your example
public class VmState
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Computer { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string Computer1 { get; set; } = string.Empty;
    public DateTime LastUpdateApplied { get; set; }
    public int OldestMissingSecurityUpdateInDays { get; set; }
    public string WindowsUpdateSetting { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public int CriticalUpdatesMissing { get; set; }
    public int SecurityUpdatesMissing { get; set; }
    public int OtherUpdatesMissing { get; set; }
    public int TotalUpdatesMissing { get; set; }
    public bool RestartPending { get; set; }
}

// Extension methods for easier data access
public static class KustoResultExtensions
{
    public static List<VmState> GetVmStateResults(this KustoResult result)
    {
        var primaryTable = result.Tables.FirstOrDefault();
        if (primaryTable == null) return new List<VmState>();

        var results = new List<VmState>();

        foreach (var row in primaryTable.Rows)
        {
            if (row.Count >= 14)
            {
                results.Add(new VmState
                {
                    SubscriptionId = row[0]?.ToString() ?? string.Empty,
                    Resource = row[1]?.ToString() ?? string.Empty,
                    Computer = row[2]?.ToString() ?? string.Empty,
                    ResourceId = row[3]?.ToString() ?? string.Empty,
                    Computer1 = row[4]?.ToString() ?? string.Empty,
                    LastUpdateApplied = DateTime.TryParse(row[5]?.ToString(), out var lastUpdate) ? lastUpdate : DateTime.MinValue,
                    OldestMissingSecurityUpdateInDays = int.TryParse(row[6]?.ToString(), out var oldestMissing) ? oldestMissing : 0,
                    WindowsUpdateSetting = row[7]?.ToString() ?? string.Empty,
                    OsVersion = row[8]?.ToString() ?? string.Empty,
                    CriticalUpdatesMissing = int.TryParse(row[9]?.ToString(), out var criticalMissing) ? criticalMissing : 0,
                    SecurityUpdatesMissing = int.TryParse(row[10]?.ToString(), out var securityMissing) ? securityMissing : 0,
                    OtherUpdatesMissing = int.TryParse(row[11]?.ToString(), out var otherMissing) ? otherMissing : 0,
                    TotalUpdatesMissing = int.TryParse(row[12]?.ToString(), out var totalMissing) ? totalMissing : 0,
                    RestartPending = bool.TryParse(row[13]?.ToString(), out var restartPending) && restartPending
                });
            }
        }

        return results;
    }

    public static List<VmInPendingState> GetPendingVmsInfo(this KustoResult result)
    {
        var primaryTable = result.Tables.FirstOrDefault();
        if (primaryTable == null) return new List<VmInPendingState>();

        var results = new List<VmInPendingState>();

        foreach (var row in primaryTable.Rows)
        {
            if (row.Count >= 6)
            {
                results.Add(new VmInPendingState
                {
                    Id = row[0]?.ToString() ?? string.Empty,
                    Name = row[1]?.ToString() ?? string.Empty,
                    SubscriptionId = row[2]?.ToString() ?? string.Empty,
                    ResourceGroup = row[3]?.ToString() ?? string.Empty,
                    Owner = row[4]?.ToString() ?? string.Empty,
                    Contact = row[5]?.ToString() ?? string.Empty
                });
            }
        }

        return results;
    }

}