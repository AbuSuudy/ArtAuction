import { nspResourceAccess } from '../Types/types.bicep'

@description('Network Security Perimeter Name')
param nspName string

@description('Profile name for the perimeter')
param profileName string = 'default'

@description('Resources to associate with the perimeter profile, each with its own access mode (Learning | Enforced | Audit)')
param associatedResources nspResourceAccess[]

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
        id: resourceId(resource.resourceType, resource.name)
      }
      profile: {
        id: profile.id
      }
      accessMode: resource.accessMode
    }
  }
]

resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: nsp
  name: 'nsp-diagnostics'
  properties: {
    workspaceId: logAnalyticsWorkspaceResourceId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource nspAccessLogsRetention 'Microsoft.OperationalInsights/workspaces/tables@2023-09-01' = {
  parent: logAnalyticsWorkspace
  name: 'NSPAccessLogs'
  properties: {
    retentionInDays: nspAccessLogsRetentionInDays
  }
}

output resourceId string = nsp.id
output profileId string = profile.id
