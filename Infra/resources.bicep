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

@description('Key Vault Name')
param keyVaultName string

@description('Network Security Perimeter Name')
param nspName string

module keyVault 'Modules/keyVault.bicep' = {
  name: keyVaultName
  params: {
    keyVaultName: keyVaultName
  }
}

module logAnalytics 'Modules/logAnalytics.bicep' = {
  name: logAnalyticsName
  params: {
    logAnalyticsName: logAnalyticsName
  }
}

module appInsights 'Modules/appInsight.bicep' = {
  name: appInsightsName
  params: {
    appInsightsName: appInsightsName
    workspaceResourceId: logAnalytics.outputs.resourceId
  }
}

module containerRegistry 'Modules/containerRegistry.bicep' = {
  name: acrName
  params: {
    acrName: acrName
  }
}

module containerEnvironment 'Modules/containerEnvironment.bicep' = {
  name: environmentName
  params: {
    environmentName: environmentName
    logAnalyticsWorkspaceResourceId: logAnalytics.outputs.resourceId
    appInsightsConnectionString: appInsights.outputs.appInsightsConnectionString
  }
}

var containerApps = [
  { name: 'art-auction-api', disableIngress: false, activeRevisionsMode: 'Multiple', minReplicas: 2, maxReplicas: 3 }
  { name: 'art-auction-consumer', disableIngress: true, activeRevisionsMode: 'Single', minReplicas: 1, maxReplicas: 1 }
]

module container 'Modules/containerApps.bicep' = [
  for (containerApp, i) in containerApps: {
    name: 'container-${containerApp.name}'
    params: {
      containerAppsName: containerApp.name
      containerRegsistryResourceId: containerRegistry.outputs.containerRegistryResourceId
      workspaceResourceId: logAnalytics.outputs.resourceId
      environmentResourceId: containerEnvironment.outputs.resourceId
      appInsightsConnectionString: appInsights.outputs.appInsightsConnectionString
      disableIngress: containerApp.disableIngress
      activeRevisionsMode : containerApp.activeRevisionsMode
      minReplicas: containerApp.minReplicas
      maxReplicas: containerApp.maxReplicas
    }
  }
]

var identites = map(container, c => c.outputs.systemAssignedMIPrincipalId)
module serviceBus 'Modules/serviceBus.bicep' = {
  name: serviceBusName
  params: {
    serviceBusName: serviceBusName
    identites: identites
  }
}

var apiAppIndex = indexOf(map(containerApps, a => a.name), 'art-auction-consumer')

module storageAccount 'Modules/storageAccount.bicep' = {
  name: storageAccountName
  params: {
    storageAccountName: storageAccountName
    containerIdentity: container[apiAppIndex].outputs.systemAssignedMIPrincipalId
  }
}

//External container apps need to talk to log analytics works space to needs to be more lenient on the access mode workspace.
module networkSecurityPerimeter 'Modules/networkSecurityPerimeter.bicep' = {
  name: nspName
  params: {
    nspName: nspName
    associatedResources: [
      { name: keyVaultName, resourceId: keyVault.outputs.resourceId, accessMode: 'Enforced' }
      { name: serviceBusName, resourceId: serviceBus.outputs.resourceId, accessMode: 'Enforced' }
      { name: logAnalyticsName, resourceId: logAnalytics.outputs.resourceId, accessMode: 'Learning' }
      { name: storageAccountName, resourceId: storageAccount.outputs.resourceId, accessMode: 'Enforced' }
    ]
    logAnalyticsWorkspaceName: logAnalyticsName
    logAnalyticsWorkspaceResourceId: logAnalytics.outputs.resourceId
  }
}
