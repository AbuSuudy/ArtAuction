using '../main.bicep'

param logAnalyticsName = 'LAW-ARTAUCTION-NEU-001'
param appInsightsName = 'API-ARTAUCTION-NEU-001'
param acrName = 'crartauctionneu001'
param environmentName = 'CAE-ARTAUCTION-NEU-001'
param resourceGroupName = 'RG-ARTAUCTION-NEU-001'
param serviceBusName = 'SBN-ARTAUCTION-NEU-001'
param storageAccountName = 'startauctionneu001'
param keyVaultName = 'KV-ARTAUCTION-NEU-001'
param nspName = 'NSP-ARTAUCTION-NEU-001'

@description('Every queue the consumer side (ArtWorkBidConsumer + its routing-slip activities) needs. Created up front so the KEDA scale rules below always have something to watch, instead of waiting on MassTransit to create them at runtime.')
var consumerQueueNames = [
  'artworkbid'
  'bidaccepted-execute'
  'bidaccepted-compensate'
  'bidrejected-execute'
  'bidrejected-compensate'
  'aquired-execute'
  'aquired-compensate'
]

param serviceBusQueueNames = consumerQueueNames

param containerApps = [
  {
    name: 'ca-artauctionapi-neu-001'
    disableIngress: false
    activeRevisionsMode: 'Multiple'
    minReplicas: 1
    maxReplicas: 3
    scaleQueueNames: []
    env: [
      { name: 'OTEL_SERVICE_NAME', value: 'ca-artauctionapi-neu-001' }
      { name: 'ServiceBus__Host', value: 'sb://${serviceBusName}.servicebus.windows.net' }
      { name: 'DOTNET_ENVIRONMENT', value: 'Development' }
      { name: 'ASPNETCORE_ENVIRONMENT', value: 'Development' }
    ]
  }
  {
    name: 'ca-artauctionconsumer-neu-001'
    disableIngress: true
    activeRevisionsMode: 'Single'
    minReplicas: 0
    maxReplicas: 1
    scaleQueueNames: consumerQueueNames
    env: [
      { name: 'OTEL_SERVICE_NAME', value: 'ca-artauctionconsumer-neu-001' }
      { name: 'ServiceBus__Host', value: 'sb://${serviceBusName}.servicebus.windows.net' }
      { name: 'StorageAccount__TableUri', value: 'https://${storageAccountName}.table.core.windows.net' }
      { name: 'DOTNET_ENVIRONMENT', value: 'Development' }
      { name: 'ASPNETCORE_ENVIRONMENT', value: 'Development' }
    ]
  }
]

//External container apps need to talk to the log analytics workspace so it needs a more lenient access mode.
param nspAssociatedResources = [
  { name: keyVaultName, resourceType: 'Microsoft.KeyVault/vaults', accessMode: 'Enforced' }
  { name: serviceBusName, resourceType: 'Microsoft.ServiceBus/namespaces', accessMode: 'Enforced' }
  { name: logAnalyticsName, resourceType: 'Microsoft.OperationalInsights/workspaces', accessMode: 'Learning' }
  { name: storageAccountName, resourceType: 'Microsoft.Storage/storageAccounts', accessMode: 'Enforced' }
]
