import { containerAppConfig, nspResourceAccess } from 'Types/types.bicep'

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

@description('Container Apps to deploy')
param containerApps containerAppConfig[]

@description('Resources to associate with the Network Security Perimeter')
param nspAssociatedResources nspResourceAccess[]

@description('Map of container app name to the image it is currently running, so an infra-only deploy does not reset it to the placeholder image')
param imageOverrides object = {}

@description('Service Bus queue names to create on the namespace')
param serviceBusQueueNames string[] = []

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

module serviceBus 'Modules/serviceBus.bicep' = {
  name: serviceBusName
  params: {
    serviceBusName: serviceBusName
    identites: container.outputs.systemAssignedMIPrincipalIds
    queueNames: serviceBusQueueNames
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

module container 'Modules/containerApps.bicep' = {
  name: 'container-apps'
  params: {
    containerApps: containerApps
    workspaceResourceId: logAnalytics.outputs.resourceId
    environmentResourceId: containerEnvironment.outputs.resourceId
    appInsightsConnectionString: appInsights.outputs.appInsightsConnectionString
    serviceBusName: serviceBusName
    acrName: acrName
    imageOverrides: imageOverrides
  }
}

module containerRegistry 'Modules/containerRegistry.bicep' = {
  name: acrName
  params: {
    acrName: acrName
    identites: container.outputs.systemAssignedMIPrincipalIds
  }
}

var apiAppIndex = indexOf(map(containerApps, a => a.name), 'ca-artauctionconsumer-neu-001')

module storageAccount 'Modules/storageAccount.bicep' = {
  name: storageAccountName
  params: {
    storageAccountName: storageAccountName
    containerIdentity: container.outputs.systemAssignedMIPrincipalIds[apiAppIndex]
  }
}

module networkSecurityPerimeter 'Modules/networkSecurityPerimeter.bicep' = {
  name: nspName
  params: {
    nspName: nspName
    associatedResources: nspAssociatedResources
    logAnalyticsWorkspaceName: logAnalyticsName
    logAnalyticsWorkspaceResourceId: logAnalytics.outputs.resourceId
  }
  dependsOn: [
    keyVault
    serviceBus
    storageAccount
  ]
}
