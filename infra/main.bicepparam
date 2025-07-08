using 'main.bicep'

param location = 'canadacentral'

param resourceGroupName = 'rg-logic-app-automation'

param subnetAddressPrefix = '172.16.1.0/28'

param subnetName = 'snet-vms'

param virtualNetworkAddressPrefix = '172.16.0.0/16'

param virtualNetworkName = 'vnet-logic-app'

param adminPassword = ''

param adminUsername = ''
