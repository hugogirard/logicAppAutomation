# VM Pending State Monitoring Azure Function

This Azure Function application monitors Virtual Machines (VMs) in Azure that are in a pending reboot state and automatically sends email notifications to VM owners. The function queries a Log Analytics workspace to identify VMs requiring reboots and sends personalized notifications to the designated contacts.

## Overview

The application consists of:
- A timer-triggered Azure Function that runs on a scheduled basis
- Integration with Azure Monitor/Log Analytics to query VM update status
- Microsoft Graph API integration for sending emails via Office 365
- VM resource querying using Azure Resource Graph API

## Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│  Timer Trigger  │───▶│  Azure Function  │───▶│ Log Analytics   │
│   (Scheduled)   │    │                  │    │   Workspace     │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌──────────────────┐    ┌─────────────────┐
                       │ Azure Resource   │───▶│ Microsoft Graph │
                       │     Graph        │    │   (Email API)   │
                       └──────────────────┘    └─────────────────┘
```

## Prerequisites

### Azure Resources Required
1. **Azure Function App** (with .NET 8 runtime)
2. **Log Analytics Workspace** (with VM update data)
3. **Azure App Registration** (for Microsoft Graph API access)
4. **Virtual Machines** with proper tags

### Required Tags on VMs
Each VM that should be monitored must have the following tags:
- `owner`: Name of the VM owner
- `contact`: Valid email address for notifications

## Setup Instructions

### 1. Create Azure App Registration

Create an app registration for Microsoft Graph API access using the Azure Portal:

1. Navigate to **Azure Portal** → **Azure Active Directory** → **App registrations**
2. Click **New registration**
3. Fill in the details:
   - **Name**: `VM-Monitoring-Function`
   - **Supported account types**: Select "Accounts in this organizational directory only"
   - **Redirect URI**: Leave blank
4. Click **Register**
5. On the app registration overview page, note down:
   - **Application (client) ID**
   - **Directory (tenant) ID**
6. Go to **Certificates & secrets** → **Client secrets**
7. Click **New client secret**
8. Add a description (e.g., "VM Monitoring Function Secret")
9. Set expiration (recommended: 24 months)
10. Click **Add** and **immediately copy the secret value** (you won't see it again)

### 2. Configure Microsoft Graph API Permissions

Add required permissions to the app registration:

1. Go to Azure Portal → Azure Active Directory → App registrations
2. Find your "VM-Monitoring-Function" app
3. Go to "API permissions"
4. Add the following Microsoft Graph **Application** permissions:
   - `Mail.Send`
   - `User.Read.All` (if sending from a shared mailbox)
5. Click "Grant admin consent"

### 3. Create Azure Function App

Create the Function App using the Azure Portal:

1. Navigate to **Azure Portal** → **Create a resource**
2. Search for **Function App** and select it
3. Click **Create** and fill in the details:
   
   **Basics tab:**
   - **Subscription**: Select your subscription
   - **Resource Group**: Create new → `rg-vm-monitoring`
   - **Function App name**: `func-vm-monitoring-[unique-suffix]`
   - **Publish**: Code
   - **Runtime stack**: .NET
   - **Version**: 8 (LTS) Isolated
   - **Region**: East US (or your preferred region)
   
   **Hosting tab:**
   - **Storage account**: Create new (default name is fine)
   - **Operating System**: Windows
   - **Plan type**: Choose the want you needs
   
4. Click **Review + create** then **Create**
5. Wait for deployment to complete

### 4. Configure Managed Identity

Enable system-assigned managed identity for the Function App:

1. Navigate to your Function App in the Azure Portal
2. Go to **Settings** → **Identity**
3. Under **System assigned** tab:
   - Toggle **Status** to **On**
   - Click **Save**
   - Click **Yes** to confirm
4. Note down the **Object (principal) ID** that appears - you'll need this for role assignments

### 5. Assign RBAC Permissions

#### Subscription Level (for VM Resource Graph queries)

1. Navigate to **Azure Portal** → **Subscriptions**
2. Select the subscription containing your VMs
3. Go to **Access control (IAM)**
4. Click **Add** → **Add role assignment**
5. Select **Reader** role
6. Click **Next**
7. Under **Assign access to**: Select **Managed identity**
8. Click **Select members**
9. Choose your subscription
10. Select **Function App** from the managed identity dropdown
11. Find and select your Function App (`func-vm-monitoring-xxxxx`)
12. Click **Select** → **Review + assign** → **Assign**

#### Log Analytics Workspace Level

1. Navigate to your **Log Analytics workspace**
2. Go to **Access control (IAM)**
3. Click **Add** → **Add role assignment**
4. For the first role assignment:
   - Select **Monitoring Reader** role
   - Follow steps 6-12 from above to assign it to your Function App's managed identity
5. Repeat the process for the second role:
   - Select **Log Analytics Reader** role
   - Follow steps 6-12 from above to assign it to your Function App's managed identity

### 6. Configure Application Settings

Set the application settings in your Function App:

1. Navigate to your Function App in the Azure Portal
2. Go to **Settings** → **Environment variables**
3. Under **App settings** tab, add the following settings by clicking **Add**:

| Name | Value | Description |
|------|--------|-------------|
| `workspaceID` | `your-workspace-id` | Your Log Analytics Workspace ID (found in workspace Overview) |
| `CheckVmPendingStateOnScheduleCron` | `0 0 8 * * *` | Cron expression (daily at 8 AM UTC) |
| `SenderEmail` | `your-sender@yourdomain.com` | Email address to send notifications from |
| `TenantID` | `your-tenant-id` | From your app registration |
| `ClientID` | `your-client-id` | From your app registration |
| `ClientSecret` | `your-client-secret` | From your app registration |
| `GetVMQuery` | See KQL query below | Query to identify VMs needing reboot |

**Sample GetVMQuery value for TESTING ONLY (uses mock data):**

⚠️ **IMPORTANT**: This is a hardcoded mock query for testing purposes only. It creates fake VM data and should NOT be used in production. Replace this with your actual Log Analytics query once testing is complete.

🔧 **CRITICAL**: Before using this mock query, you **MUST** customize it with your actual Azure resources:
1. Replace the subscription ID `f47ac10b-58cc-4372-a567-0e02b2c3d479` with your actual subscription ID
2. Replace the VM Resource IDs with your actual test VM Resource IDs (see instructions below)
3. Update VM names to match your actual test VMs

**How to get your VM Resource IDs from Azure Portal:**
1. Navigate to **Azure Portal** → **Virtual machines**
2. Select a VM you want to test with
3. Go to **Settings** → **Properties**
4. Copy the **Resource ID** (it looks like: `/subscriptions/YOUR-SUB-ID/resourceGroups/YOUR-RG/providers/Microsoft.Compute/virtualMachines/YOUR-VM-NAME`)
5. Repeat for each VM you want to include in testing

Otherwise, the Resource Graph queries will fail to find your VMs.

```
let MockVMInfo = datatable( SubscriptionId: string, Resource: string, Computer: string, ResourceId: string ) [ 'YOUR-SUBSCRIPTION-ID', 'YOUR-VM-NAME-1', 'YOUR-VM-NAME-1', 'YOUR-ACTUAL-VM-RESOURCE-ID-1', 'YOUR-SUBSCRIPTION-ID', 'YOUR-VM-NAME-2', 'YOUR-VM-NAME-2', 'YOUR-ACTUAL-VM-RESOURCE-ID-2', 'YOUR-SUBSCRIPTION-ID', 'YOUR-VM-NAME-3', 'YOUR-VM-NAME-3', 'YOUR-ACTUAL-VM-RESOURCE-ID-3' ]; let MockPatchSummary = datatable( Computer: string, Computer1: string, LastUpdateApplied: datetime, OldestMissingSecurityUpdateInDays: int, WindowsUpdateSetting: string, OsVersion: string, CriticalUpdatesMissing: int, SecurityUpdatesMissing: int, OtherUpdatesMissing: int, TotalUpdatesMissing: int, RestartPending: bool ) [ 'YOUR-VM-NAME-1', 'YOUR-VM-NAME-1', datetime(2025-07-01 10:30:00), 5, 'Automatic', 'Windows Server 2022', 2, 3, 1, 6, true, 'YOUR-VM-NAME-2', 'YOUR-VM-NAME-2', datetime(2025-07-03 14:15:00), 3, 'Manual', 'Windows Server 2019', 1, 2, 0, 3, false, 'YOUR-VM-NAME-3', 'YOUR-VM-NAME-3', datetime(2025-07-05 09:45:00), 7, 'Automatic', 'Windows Server 2022', 0, 1, 2, 3, true ]; MockVMInfo | join kind=leftouter (MockPatchSummary) on Computer | project SubscriptionId, Resource, Computer, ResourceId, Computer1, LastUpdateApplied, OldestMissingSecurityUpdateInDays, WindowsUpdateSetting, OsVersion, CriticalUpdatesMissing, SecurityUpdatesMissing, OtherUpdatesMissing, TotalUpdatesMissing, RestartPending
```

4. Click **Apply** to save all settings

### 7. Tag Your Virtual Machines

Ensure all VMs you want to monitor have the required tags:

1. Navigate to **Azure Portal** → **Virtual machines**
2. Select a VM you want to monitor
3. Go to **Settings** → **Tags**
4. Add the following tags:
   - **Key**: `owner`, **Value**: `John Doe` (or actual owner name)
   - **Key**: `contact`, **Value**: `john.doe@company.com` (valid email address)
5. Click **Apply**
6. Repeat for all VMs you want to monitor

**Alternative - Bulk tagging:**
1. Navigate to **Azure Portal** → **Resource groups**
2. Select the resource group containing your VMs
3. Use the checkbox to select multiple VMs
4. Click **Assign tags** at the top
5. Add the required tags for all selected VMs

### 8. Deploy the Function Code

**Recommended for Production:** Use CI/CD pipelines (GitHub Actions or Azure DevOps) for automated deployments.

**Quick Deployment Options:**
- **Visual Studio:** Right-click project → Publish → Azure Function App
- **VS Code:** Install Azure Functions extension → Deploy to Function App command

**Verify deployment:** Check that `CheckVmPendingStateOnSchedule` appears in your Function App's Functions list.

## Configuration Details

### Application Settings Reference

| Setting | Description | Example |
|---------|-------------|---------|
| `workspaceID` | Log Analytics Workspace ID | `c7582ac3-f33b-4445-92af-e87089933c1e` |
| `GetVMQuery` | KQL query to identify VMs needing reboot | See query examples below |
| `CheckVmPendingStateOnScheduleCron` | Cron expression for schedule | `0 0 8 * * *` (daily at 8 AM) |
| `SenderEmail` | Email address to send from | `admin@company.com` |
| `TenantID` | Azure AD Tenant ID | From app registration |
| `ClientID` | App Registration Client ID | From app registration |
| `ClientSecret` | App Registration Client Secret | From app registration |

### Sample KQL Queries

#### Required Project Fields for GetVMQuery

Your KQL query **must** project the following fields in this exact order for the application to work:

1. `SubscriptionId` (string) - row[0]
2. `Resource` (string) - row[1] 
3. `Computer` (string) - row[2]
4. `ResourceId` (string) - row[3]
5. `Computer1` (string) - row[4]
6. `LastUpdateApplied` (datetime) - row[5]
7. `OldestMissingSecurityUpdateInDays` (int) - row[6]
8. `WindowsUpdateSetting` (string) - row[7]
9. `OsVersion` (string) - row[8]
10. `CriticalUpdatesMissing` (int) - row[9]
11. `SecurityUpdatesMissing` (int) - row[10]
12. `OtherUpdatesMissing` (int) - row[11]
13. `TotalUpdatesMissing` (int) - row[12]
14. `RestartPending` (bool) - row[13]

**Important**: The query must project exactly these 14 fields in this order, as the code expects specific positions.

#### Production Query (customize based on your Update Management setup):

**This is your REAL production query** - replace the mock query with something like this once testing is complete:

```kql
UpdateSummary
| where Classification == "Critical Updates" or Classification == "Security Updates" 
| where UpdateState == "Needed" and Optional == false 
| where RestartPending == true
| project SubscriptionId, Resource, Computer, ResourceId, Computer, LastUpdateApplied, OldestMissingSecurityUpdateInDays, WindowsUpdateSetting, OsVersion, CriticalUpdatesMissing, SecurityUpdatesMissing, OtherUpdatesMissing, TotalUpdatesMissing, RestartPending
```

**CRITICAL**: This query **MUST** project exactly these 14 fields in this exact order. The application code expects specific field positions and will fail if the order is different.

#### Mock/Testing Query (for development and testing):

⚠️ **WARNING: This query uses HARDCODED mock data and should ONLY be used for testing!**

This query creates fake VM data to test your application without requiring actual Update Management data in your Log Analytics workspace. **DO NOT use this in production - replace it with your actual production query once testing is complete.**

**Detailed breakdown of the mock query:**

```kql
// First datatable: Creates mock VM information
let MockVMInfo = datatable( SubscriptionId: string, Resource: string, Computer: string, ResourceId: string ) 
[ 
    // 🔧 Replace with YOUR actual values:
    // SubscriptionId: Your Azure subscription ID
    // Resource & Computer: Your actual VM names  
    // ResourceId: Full resource ID from Azure Portal (VM → Properties → Resource ID)
    'YOUR-SUBSCRIPTION-ID', 'YOUR-VM-NAME-1', 'YOUR-VM-NAME-1', 'YOUR-ACTUAL-VM-RESOURCE-ID-1',
    'YOUR-SUBSCRIPTION-ID', 'YOUR-VM-NAME-2', 'YOUR-VM-NAME-2', 'YOUR-ACTUAL-VM-RESOURCE-ID-2',
    'YOUR-SUBSCRIPTION-ID', 'YOUR-VM-NAME-3', 'YOUR-VM-NAME-3', 'YOUR-ACTUAL-VM-RESOURCE-ID-3'
]; 

// Second datatable: Creates mock patch/update information
let MockPatchSummary = datatable( Computer: string, Computer1: string, LastUpdateApplied: datetime, OldestMissingSecurityUpdateInDays: int, WindowsUpdateSetting: string, OsVersion: string, CriticalUpdatesMissing: int, SecurityUpdatesMissing: int, OtherUpdatesMissing: int, TotalUpdatesMissing: int, RestartPending: bool ) 
[ 
    // 🔧 Update Computer names to match your VMs above
    // VM 1 - HAS pending restart (RestartPending: true) - will send email
    'YOUR-VM-NAME-1', 'YOUR-VM-NAME-1', datetime(2025-07-01 10:30:00), 5, 'Automatic', 'Windows Server 2022', 2, 3, 1, 6, true, 
    
    // VM 2 - NO pending restart (RestartPending: false) - will NOT send email
    'YOUR-VM-NAME-2', 'YOUR-VM-NAME-2', datetime(2025-07-03 14:15:00), 3, 'Manual', 'Windows Server 2019', 1, 2, 0, 3, false, 
    
    // VM 3 - HAS pending restart (RestartPending: true) - will send email
    'YOUR-VM-NAME-3', 'YOUR-VM-NAME-3', datetime(2025-07-05 09:45:00), 7, 'Automatic', 'Windows Server 2022', 0, 1, 2, 3, true 
]; 

// Join the mock data and project the required 14 fields in EXACT order
MockVMInfo 
| join kind=leftouter (MockPatchSummary) on Computer 
| project SubscriptionId, Resource, Computer, ResourceId, Computer1, LastUpdateApplied, OldestMissingSecurityUpdateInDays, WindowsUpdateSetting, OsVersion, CriticalUpdatesMissing, SecurityUpdatesMissing, OtherUpdatesMissing, TotalUpdatesMissing, RestartPending
```

**Key points about this mock query:**

1. **🔧 MUST REPLACE ALL PLACEHOLDER VALUES** with your actual Azure resources:
   - **YOUR-SUBSCRIPTION-ID**: Replace with your Azure subscription ID (3 instances)
   - **YOUR-VM-NAME-1/2/3**: Replace with your actual VM names (6 instances total)
   - **YOUR-ACTUAL-VM-RESOURCE-ID-1/2/3**: Replace with full Resource IDs from Azure Portal (3 instances)

2. **How to get VM Resource IDs from Azure Portal:**
   - Go to **Azure Portal** → **Virtual machines** → Select your VM
   - Go to **Settings** → **Properties**
   - Copy the full **Resource ID** (e.g., `/subscriptions/abc123.../resourceGroups/myRG/providers/Microsoft.Compute/virtualMachines/myVM`)

3. **RestartPending Status**: VMs 1 and 3 have `RestartPending: true` (will send emails), VM 2 has `false` (no email)
4. **Field Order is CRITICAL**: The final `project` statement must maintain the exact 14-field order
5. **VM Tags Required**: Ensure your actual VMs have `owner` and `contact` tags for emails to be sent

**Steps to customize the mock query for testing:**
1. **Get your Subscription ID**: Azure Portal → Subscriptions → copy the Subscription ID
2. **Get VM Resource IDs**: For each test VM → Properties → copy Resource ID  
3. **Replace placeholders**: Update all YOUR-SUBSCRIPTION-ID, YOUR-VM-NAME-X, and YOUR-ACTUAL-VM-RESOURCE-ID-X values
4. **Add required tags**: Ensure your VMs have `owner` and `contact` tags
5. **Test the query**: Run it in Log Analytics to verify it returns 3 rows with 14 columns

**Example of properly formatted values:**
- SubscriptionId: `f47ac10b-58cc-4372-a567-0e02b2c3d479`
- VM Name: `webserver01`
- Resource ID: `/subscriptions/f47ac10b-58cc-4372-a567-0e02b2c3d479/resourceGroups/rg-production/providers/Microsoft.Compute/virtualMachines/webserver01`

**After testing, replace this entire mock query with a real query that reads from your UpdateSummary or Update tables in Log Analytics.**

#### Alternative Query for Azure Update Manager:

**Another example of a production query** for environments using Azure Update Manager:

```kql
Update
| where UpdateState == "Needed" and Classification in ("Critical Updates", "Security Updates")
| where RestartPending == true
| extend ResourceId = strcat("/subscriptions/", SubscriptionId, "/resourceGroups/", ResourceGroup, "/providers/Microsoft.Compute/virtualMachines/", Computer)
| project SubscriptionId, Resource, Computer, ResourceId, Computer, LastUpdateApplied, OldestMissingSecurityUpdateInDays, WindowsUpdateSetting, OsVersion, CriticalUpdatesMissing, SecurityUpdatesMissing, OtherUpdatesMissing, TotalUpdatesMissing, RestartPending
```

**Important**: Ensure all 14 fields exist in your Log Analytics schema before using these production queries. Test your query in Log Analytics first to verify it returns the expected data structure.

#### VM Resource Graph Query (for VM details)

The application also uses Azure Resource Graph to get VM owner information. This query **must** project these fields in order:

1. `id` (string) - row[0]
2. `name` (string) - row[1]
3. `subscriptionId` (string) - row[2]
4. `resourceGroup` (string) - row[3]
5. `owner` (string) - row[4] - from VM tags
6. `contact` (string) - row[5] - from VM tags

This query is automatically generated by the code:
```kql
arg('').resources 
| where type contains 'microsoft.compute/virtualmachines'
| where tolower(id) == tolower('[ResourceId]')
| project id, name, subscriptionId, resourceGroup, owner = tostring(tags['owner']), contact = tostring(tags['contact'])
```

### Schedule Configuration

The function uses cron expressions for scheduling:
- `0 0 8 * * *` - Daily at 8:00 AM
- `0 0 8 * * MON` - Every Monday at 8:00 AM  
- `0 0 8,16 * * *` - Twice daily at 8:00 AM and 4:00 PM

## Security Considerations

1. **Managed Identity**: The Function App uses managed identity for Azure Resource Graph and Log Analytics access
2. **App Registration**: Uses app registration with client secret for Microsoft Graph (email sending)
3. **Least Privilege**: Only required permissions are assigned
4. **Secrets Management**: Store sensitive values in Azure Key Vault for production

## Monitoring and Troubleshooting

### Application Insights
The function includes Application Insights integration for monitoring:
- Function execution logs
- Performance metrics
- Error tracking
- Dependencies

### Common Issues

1. **No VMs found**: Check if VMs have the required tags (`owner` and `contact`)
2. **Email not sending**: Verify app registration permissions and sender email configuration
3. **Access denied to Log Analytics**: Check RBAC assignments for the managed identity
4. **Query returns no results**: Verify the KQL query matches your Update Management data structure

### Logs and Debugging

View and monitor function execution:

1. **Function App Logs:**
   - Navigate to your Function App → **Monitoring** → **Log stream**
   - This shows real-time logs from your function

2. **Application Insights:**
   - Go to your Function App → **Monitoring** → **Application Insights**
   - View detailed telemetry, performance metrics, and error tracking

3. **Function Execution History:**
   - Navigate to **Functions** → **CheckVmPendingStateOnSchedule** → **Monitor**
   - View execution history, success/failure status, and execution logs

4. **Test the Function:**
   - Go to **Functions** → **CheckVmPendingStateOnSchedule** → **Code + Test**
   - Click **Test/Run** to manually trigger the function for testing

## Customization

### Email Template
Modify the email content in `GraphService.SendEmailVmPendingState()` method:

```csharp
Subject = $"VM {vm.Name} needs to be rebooted",
Content = $"Hi {vm.Owner}, the VM '{vm.Name}' is in a pending state. SubscriptionID: {vm.SubscriptionId} - ResourceGroup: {vm.ResourceGroup}"
```

### Query Logic
Update the KQL query in application settings to match your specific Log Analytics schema and requirements.

### Additional VM Information
Extend the `VmInPendingState` model to include additional VM properties like location, size, etc.

## Dependencies

- .NET 8.0
- Azure Functions v4
- Azure.Identity (for managed identity)
- Microsoft.Graph (for email sending)
- Application Insights (for monitoring)

## License

MIT