@description('Network Security Perimeter Name')
param nspName string

@description('Profile name for the perimeter')
param profileName string = 'default'

@description('Resources to associate with the perimeter profile')
param associatedResources array

@description('Access mode for the resource associations')
@allowed([
  'Learning'
  'Enforced'
  'Audit'
])
param accessMode string = 'Enforced'

resource nsp 'Microsoft.Network/networkSecurityPerimeters@2024-07-01' = {
  name: nspName
  location: resourceGroup().location
  properties: {}
}

resource profile 'Microsoft.Network/networkSecurityPerimeters/profiles@2024-07-01' = {
  parent: nsp
  name: profileName
  properties: {}
}

//Allow inbound access from any managed identity in the current subscription
resource inboundSameSubscriptionRule 'Microsoft.Network/networkSecurityPerimeters/profiles/accessRules@2024-07-01' = {
  parent: profile
  name: 'allow-inbound-same-subscription'
  properties: {
    direction: 'Inbound'
    subscriptions: [
      {
        id: subscription().id
      }
    ]
  }
}

resource associations 'Microsoft.Network/networkSecurityPerimeters/resourceAssociations@2024-07-01' = [
  for resource in associatedResources: {
    parent: nsp
    name: '${resource.name}-association'
    properties: {
      privateLinkResource: {
        id: resource.resourceId
      }
      profile: {
        id: profile.id
      }
      accessMode: accessMode
    }
  }
]

output resourceId string = nsp.id
output profileId string = profile.id
