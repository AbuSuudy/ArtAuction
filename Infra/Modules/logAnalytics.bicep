@description('Log Analytics Workspace Name')
param logAnalyticsName string

module workspace 'br/public:avm/res/operational-insights/workspace:0.15.1' = {
  name: '${logAnalyticsName}-${uniqueString(resourceGroup().id, logAnalyticsName)}'
  params: {
    name: logAnalyticsName
    managedIdentities: {
      systemAssigned: true
    }
  }
}

output resourceId string = workspace.outputs.resourceId
