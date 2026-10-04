//az deployment sub create --name ExampleDeployment --location northeurope --template-file Infra\main.bicep --parameters Infra\Params\main.bicepparam
import { containerAppConfig, nspResourceAccess } from 'Types/types.bicep'

targetScope = 'subscription'

@description('Log Analytics Workspace Name')
param logAnalyticsName string

@description('The name of the Azure Container Registry')
param appInsightsName string

@description('The name of the Azure Container Registry')
param acrName string

@description('Managed Environment Name')
param environmentName string

@description('Resouce Group Name')
param resourceGroupName string

@description('Service Bus name')
param serviceBusName string

@description('Storage Account')
param storageAccountName string

@description('Key Vault Name')
param keyVaultName string

@description('Network Security Perimeter Name')
param nspName string

@description('Container Apps to deploy')
param containerApps containerAppConfig[]

@description('Resources to associate with the Network Security Perimeter')
param nspAssociatedResources nspResourceAccess[]

resource resourceGroup 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: resourceGroupName
  location: 'NorthEurope'
}

module resources 'resources.bicep' = {
  name: 'Resources'
  scope: resourceGroup
  params: {
    acrName: acrName
    appInsightsName: appInsightsName
    environmentName: environmentName
    logAnalyticsName: logAnalyticsName
    serviceBusName: serviceBusName
    storageAccountName: storageAccountName
    keyVaultName: keyVaultName
    nspName: nspName
    containerApps: containerApps
    nspAssociatedResources: nspAssociatedResources
  }
}
