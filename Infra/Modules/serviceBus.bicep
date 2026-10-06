@description('Service bus name')
param serviceBusName string
param identites array

module servicebus 'br/public:avm/res/service-bus/namespace:0.16.2' = {
  name: '${serviceBusName}-${uniqueString(deployment().name)}'
  params: {
    name: serviceBusName
    skuObject: {
      name: 'Standard'
      capacity: 1
    }
    managedIdentities: {
      systemAssigned: true
    }
  }
}

//Azure Service Bus Data Owner
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    for (identity, i) in identites: {
        scope: servicebus
        name: guid(scopeResourceId, principalId, roleDefinitionId)
        properties: {
          principalId: identity
          roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
        }
    }
}

output resourceId string = servicebus.outputs.resourceId
