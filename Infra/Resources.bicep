@description('Log Analytics Workspace Name')
param logAnalyticsName string

@description('The name of the Azure Container Registry')
param appInsightsName string

@description('The name of the Azure Container Registry')
param acrName string

@description('Managed Environment Name')
param environmentName string

@description('Service Bus name')
param serviceBusName string

@description('Storage Account')
param storageAccountName string

module logAnalytics 'Modules/LogAnalytics.bicep' = {
  name: logAnalyticsName
  params: {
    logAnalyticsName: logAnalyticsName
  }
}

module appInsights 'Modules/AppInsight.bicep' = {
  name: appInsightsName
  params: {
    appInsightsName: appInsightsName
    workspaceResourceId: logAnalytics.outputs.resourceId
  }
}

module containerRegistry 'Modules/ContainerRegistry.bicep' = {
  name: acrName
  params: {
    acrName: acrName
  }
}

module containerEnvironment 'Modules/ContainerEnvironment.bicep' = {
  name: environmentName
  params: {
    environmentName: environmentName
    logAnalyticsWorkspaceResourceId: logAnalytics.outputs.resourceId
    appInsightsConnectionString: appInsights.outputs.appInsightsConnectionString
  }
}

var containerApps = [
  { name: 'art-auction-api', exposedPort: true }
  { name: 'art-auction-consumer', exposedPort: false }
]

module container 'Modules/ContainerApps.bicep' = [
  for (containerApp, i) in containerApps: {
    name: 'container-${containerApp.name}'
    params: {
      containerAppsName: containerApp.name
      containerRegsistryResourceId: containerRegistry.outputs.containerRegistryResourceId
      workspaceResourceId: logAnalytics.outputs.resourceId
      environmentResourceId: containerEnvironment.outputs.resourceId
    }
  }
]

module serviceBus 'Modules/ServiceBus.bicep' = {
  name: serviceBusName
  params: {
    serviceBusName: serviceBusName
  }
}

module storageAccount 'Modules/storageAccount.bicep' = {
  name: storageAccountName
  params: {
    storageAccountName: storageAccountName
  }
}
