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

resource newRG 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: resourceGroupName
  location: 'NorthEurope'
}

module dfd 'Resources.bicep' = {
  name: 'Resources'
  scope: newRG
  params: {
    acrName: acrName
    appInsightsName: appInsightsName
    environmentName: environmentName
    logAnalyticsName: logAnalyticsName
    serviceBusName: serviceBusName
    storageAccountName: storageAccountName
  }
}
