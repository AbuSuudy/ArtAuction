@description('The name of the Azure Container Registry')
param appInsightsName string

@description('Log analytics workspace resource id')
param workspaceResourceId string

module appInsight 'br/public:avm/res/insights/component:0.7.2' = {
  name: '${appInsightsName}-${uniqueString(deployment().name)}'
  params: {
    name: appInsightsName
    workspaceResourceId: workspaceResourceId
  }
}

output appInsightsConnectionString string = appInsight.outputs.connectionString
