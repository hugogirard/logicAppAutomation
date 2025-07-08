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

module serverFarm 'br/public:avm/res/web/serverfarm:0.4.1' = {
  scope: rg
  params: {
    name: 'asp-${suffix}'
    maximumElasticWorkerCount: 20
    skuName: 'WS1'
  }
}

output resourceGroupName string = rg.name
