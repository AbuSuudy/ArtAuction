@description('Service bus name')
param serviceBusName string

@description('Queue names to create on the namespace')
param queueNames string[] = []

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
    queues: [
      for queueName in queueNames: {
        name: queueName
      }
    ]
  }
}

output resourceId string = servicebus.outputs.resourceId
