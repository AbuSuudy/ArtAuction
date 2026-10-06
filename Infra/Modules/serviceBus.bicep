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
    //Azure Service Bus Data Owner
    roleAssignments: [
      for identity in identites: {
        principalId: identity
        roleDefinitionIdOrName: '090c5cfd-751d-490a-894a-3ce6f1109419'
      }
    ]
  }
}

output resourceId string = servicebus.outputs.resourceId
