@description('Network Security Perimeter Name')
param nspName string

@description('Profile name for the perimeter')
param profileName string = 'default'

@description('Resources to associate with the perimeter profile, each with its own access mode (Learning | Enforced | Audit)')
param associatedResources array

@description('Log Analytics Workspace name to send NSP access logs to')
param logAnalyticsWorkspaceName string

@description('Log Analytics Workspace resource id to send NSP access logs to')
param logAnalyticsWorkspaceResourceId string

@description('Retention, in days, for the NSPAccessLogs table in the Log Analytics workspace')
param nspAccessLogsRetentionInDays int = 7

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

resource associations 'Microsoft.Network/networkSecurityPerimeters/resourceAssociations@2025-09-01' = [
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
      accessMode: resource.accessMode
    }
  }
]

module diagnostics 'nspDiagnostics.bicep' = {
  name: '${nspName}-diagnostics'
  params: {
    nspName: nsp.name
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
    logAnalyticsWorkspaceResourceId: logAnalyticsWorkspaceResourceId
    nspAccessLogsRetentionInDays: nspAccessLogsRetentionInDays
  }
}

output resourceId string = nsp.id
output profileId string = profile.id
