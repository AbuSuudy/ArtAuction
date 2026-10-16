@description('The name of the Azure Container Registry to grant AcrPull on')
param acrName string

@description('The name of the Service Bus namespace to grant Service Bus Data Owner on')
param serviceBusName string

@description('The name of the Storage Account to grant Storage Table Data Contributor on')
param storageAccountName string

@description('System-assigned managed identity principal IDs of the container apps, granted AcrPull and Service Bus Data Owner')
param containerAppPrincipalIds array

@description('System-assigned managed identity principal ID granted Storage Table Data Contributor')
param storageTablePrincipalId string

var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var serviceBusDataOwnerRoleId = '090c5cfd-751d-490a-894a-3ce6f1109419'
var storageTableDataContributorRoleId = '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: serviceBusName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

resource acrPullAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for principalId in containerAppPrincipalIds: {
    name: guid(acr.id, principalId, acrPullRoleId)
    scope: acr
    properties: {
      principalId: principalId
      principalType: 'ServicePrincipal'
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
    }
  }
]

resource serviceBusDataOwnerAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for principalId in containerAppPrincipalIds: {
    name: guid(serviceBusNamespace.id, principalId, serviceBusDataOwnerRoleId)
    scope: serviceBusNamespace
    properties: {
      principalId: principalId
      principalType: 'ServicePrincipal'
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', serviceBusDataOwnerRoleId)
    }
  }
]

resource storageTableDataContributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, storageTablePrincipalId, storageTableDataContributorRoleId)
  scope: storageAccount
  properties: {
    principalId: storageTablePrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageTableDataContributorRoleId)
  }
}
