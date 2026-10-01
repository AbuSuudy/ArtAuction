@description('Network Security Perimeter Name')
param nspName string

@description('Log Analytics Workspace name to send NSP access logs to')
param logAnalyticsWorkspaceName string

@description('Log Analytics Workspace resource id to send NSP access logs to')
param logAnalyticsWorkspaceResourceId string

@description('Retention, in days, for the NSPAccessLogs table in the Log Analytics workspace')
param nspAccessLogsRetentionInDays int = 7

resource nsp 'Microsoft.Network/networkSecurityPerimeters@2024-07-01' existing = {
  name: nspName
}

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
