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
  }
}

//Azure Service Bus Data Owner
module iams 'roleAssignments.bicep' = [
  for (identity, i) in identites: {
    name: '${serviceBusName}-Role-${uniqueString(identity)}'
    params: {
      principalId: identity
      roleDefinitionId: '090c5cfd-751d-490a-894a-3ce6f1109419'
      scopeResourceId: servicebus.outputs.resourceId
    }
  }
]

output resourceId string = servicebus.outputs.resourceId
