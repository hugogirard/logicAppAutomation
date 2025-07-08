// Mock query that returns the same structure as the original but with sample data
let MockVMInfo = datatable(
    SubscriptionId: string,
    Resource: string,
    Computer: string,
    ResourceId: string
) [
    "8d63728c-d96c-4fa0-ad3f-865b5a873d77", "webserver", "webserver", "/subscriptions/8d63728c-d96c-4fa0-ad3f-865b5a873d77/resourceGroups/rg-logicapp/providers/Microsoft.Compute/virtualMachines/webserver",
    "8d63728c-d96c-4fa0-ad3f-865b5a873d77", "databaseserver", "databaseserver", "/subscriptions/8d63728c-d96c-4fa0-ad3f-865b5a873d77/resourceGroups/rg-logicapp/providers/Microsoft.Compute/virtualMachines/databaseserver",
    "8d63728c-d96c-4fa0-ad3f-865b5a873d77", "middlewareserver", "middlewareserver", "/subscriptions/8d63728c-d96c-4fa0-ad3f-865b5a873d77/resourceGroups/rg-logicapp/providers/Microsoft.Compute/virtualMachines/middlewareserver"
];
let MockPatchSummary = datatable(
    Computer: string,
    Computer1: string,
    LastUpdateApplied: datetime,
    OldestMissingSecurityUpdateInDays: int,
    WindowsUpdateSetting: string,
    OsVersion: string,
    CriticalUpdatesMissing: int,
    SecurityUpdatesMissing: int,
    OtherUpdatesMissing: int,
    TotalUpdatesMissing: int,
    RestartPending: bool
) [
    "webserver", "webserver", datetime(2025-07-01 10:30:00), 5, "Automatic", "Windows Server 2022", 2, 3, 1, 6, true,
    "databaseserver", "databaseserver", datetime(2025-07-03 14:15:00), 3, "Manual", "Windows Server 2019", 1, 2, 0, 3, false,
    "middlewareserver", "middlewareserver", datetime(2025-07-05 09:45:00), 7, "Automatic", "Windows Server 2022", 0, 1, 2, 3, true
];
MockVMInfo
| join kind=leftouter (MockPatchSummary) on Computer
| project SubscriptionId, Resource, Computer, ResourceId, Computer1, LastUpdateApplied, OldestMissingSecurityUpdateInDays, WindowsUpdateSetting, OsVersion, CriticalUpdatesMissing, SecurityUpdatesMissing, OtherUpdatesMissing, TotalUpdatesMissing, RestartPending