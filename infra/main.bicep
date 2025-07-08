targetScope = 'subscription'

@description('The location where the resource will be created')
param location string

@description('The resource group name')
param resourceGroupName string

@description('The name of the virtual network')
param virtualNetworkName string

@description('The address prefix of the virtual network')
param virtualNetworkAddressPrefix string

@description('The name of the subnet that will contain the VMs')
param subnetName string

@description('The address prefix of the subnet that will contain the VM')
param subnetAddressPrefix string

resource rg 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: resourceGroupName
  location: location
}

/*
   Create the network that will hold the VMs
*/

module nsg 'br/public:avm/res/network/network-security-group:0.5.1' = {
  scope: rg
  params: {
    name: 'nsg-vm'
  }
}

module virtualNetwork 'br/public:avm/res/network/virtual-network:0.7.0' = {
  scope: rg
  params: {
    addressPrefixes: [
      virtualNetworkAddressPrefix
    ]
    name: virtualNetworkName
    location: location
    subnets: [
      {
        name: subnetName
        addressPrefix: subnetAddressPrefix
        networkSecurityGroupResourceId: nsg.outputs.resourceId
      }
    ]
  }
}

/*
   Create the Logic App hosting
*/
var suffix = uniqueString(rg.id)

module storage 'br/public:avm/res/storage/storage-account:0.25.0' = {
  scope: rg
  params: {
    name: 'str${replace(suffix,'-','')}'
    location: location
    kind: 'StorageV2'
    allowSharedKeyAccess: true
  }
}

module logworkspace 'br/public:avm/res/operational-insights/workspace:0.12.0' = {
  scope: rg
  params: {
    name: 'log-${suffix}'
  }
}

module appinsights 'br/public:avm/res/insights/component:0.6.0' = {
  scope: rg
  params: {
    name: 'appi-${suffix}'
    workspaceResourceId: logworkspace.outputs.resourceId
  }
}

module serverFarm 'br/public:avm/res/web/serverfarm:0.4.1' = {
  scope: rg
  params: {
    name: 'asp-${suffix}'
    maximumElasticWorkerCount: 20
    skuName: 'WS1'
  }
}

module logicapp 'br/public:avm/res/web/site:0.16.0' = {
  scope: rg
  params: {
    name: 'logic-${suffix}'
    location: location
    kind: 'functionapp,workflowapp'
    serverFarmResourceId: serverFarm.outputs.resourceId
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '~20'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appinsights.outputs.connectionString
        }
        {
          name: 'AzureWebJobsStorage'
          value: storage.outputs.primaryConnectionString
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storage.outputs.primaryConnectionString
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: replace(suffix, '-', '')
        }
        {
          name: 'AzureFunctionsJobHost__extensionBundle__id'
          value: 'Microsoft.Azure.Functions.ExtensionBundle.Workflows'
        }
        {
          name: 'AzureFunctionsJobHost__extensionBundle__version'
          value: '[1.*, 2.0.0)'
        }
        {
          name: 'APP_KIND'
          value: 'workflowApp'
        }
        {
          name: 'FUNCTIONS_INPROC_NET8_ENABLED'
          value: '1'
        }
      ]
      use32BitWorkerProcess: false
      ftpsState: 'FtpsOnly'
      netFrameworkVersion: 'v6.0'
    }
  }
}

output resourceGroupName string = rg.name
