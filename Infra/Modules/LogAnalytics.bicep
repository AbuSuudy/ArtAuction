@description('Log Analytics Workspace Name')
param logAnalyticsName string

module workspace 'br/public:avm/res/operational-insights/workspace:0.15.1' = {
  name: '${logAnalyticsName}-${uniqueString(deployment().name)}'
  params: {
    name: logAnalyticsName
  }
}

output resourceId string = workspace.outputs.resourceId
