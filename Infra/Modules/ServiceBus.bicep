@description('Service bus name')
param serviceBusName string

module namespace 'br/public:avm/res/service-bus/namespace:0.16.2' = {
  name: '${serviceBusName}-${uniqueString(deployment().name)}'
  params: {
    name: serviceBusName
    skuObject: {
      name: 'Standard'
      capacity: 1
    }
  }
}
