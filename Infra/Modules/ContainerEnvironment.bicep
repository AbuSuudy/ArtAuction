@description('Managed Environment Name')
param environmentName string

@description('App Insights Connection String')
param appInsightsConnectionString string

@description('Managed Environment Name')
param logAnalyticsWorkspaceResourceId string

module managedEnvironment 'br/public:avm/res/app/managed-environment:0.13.3' = {
  name: '${environmentName}-${uniqueString(deployment().name)}'
  params: {
    name: environmentName
    zoneRedundant: false
    publicNetworkAccess: 'Enabled'
    appInsightsConnectionString: appInsightsConnectionString
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsWorkspaceResourceId: logAnalyticsWorkspaceResourceId
    }
  }
}

output resourceId string = managedEnvironment.outputs.resourceId
