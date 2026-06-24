@description('Service bus name')
param serviceBusName string

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
module iams 'roleAssignments.bicep' = {
  name: '${serviceBusName}-Role-${uniqueString(deployment().name)}'
  params: {
    principalId: 'e9b6284d-9e6c-4e7e-af4c-369c64f12cad'
    roleDefinitionId: '090c5cfd-751d-490a-894a-3ce6f1109419'
    scopeResourceId: servicebus.outputs.resourceId
  }
}
