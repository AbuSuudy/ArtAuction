@description('Container App Name')
param containerAppsName string

@description('Environment resouce environment')
param environmentResourceId string

@description('Workspace Resource Id')
param workspaceResourceId string

module containerApp 'br/public:avm/res/app/container-app:0.22.1' = {
  name: '${containerAppsName}-${uniqueString(deployment().name)}'
  params: {
    name: containerAppsName
    diagnosticSettings: [
      {
        workspaceResourceId: workspaceResourceId
      }
    ]
    containers: [
      {
        image: 'mcr.microsoft.com/k8se/quickstart:latest'
        name: 'simple-hello-world-container'
        resources: {
          cpu: 1
          memory: '2.0Gi'
        }
      }
    ]
    activeRevisionsMode: 'Multiple'
    environmentResourceId: environmentResourceId
  }
}
