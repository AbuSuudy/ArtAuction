import { containerAppConfig } from '../Types/types.bicep'

@description('Container Apps to deploy')
param containerApps containerAppConfig[]

@description('Environment resouce environment')
param environmentResourceId string

@description('Workspace Resource Id')
param workspaceResourceId string

@description('App Insights Connection String')
param appInsightsConnectionString string

@description('Map of container app name to the image it is currently running. Apps missing from this map (e.g. on first deploy) fall back to the placeholder image.')
param imageOverrides object = {}

var placeholderImage = 'mcr.microsoft.com/k8se/quickstart:latest'

module containerApp 'br/public:avm/res/app/container-app:0.23.0' = [
  for app in containerApps: {
    name: '${app.name}-${uniqueString(deployment().name)}'
    params: {
      name: app.name
      managedIdentities: {
        systemAssigned: true
      }
      diagnosticSettings: [
        {
          workspaceResourceId: workspaceResourceId
        }
      ]
      containers: [
        {
          image: contains(imageOverrides, app.name) ? imageOverrides[app.name] : placeholderImage
          name: app.name
          resources: {
            cpu: 1
            memory: '2.0Gi'
          }
          env: concat(
            [
              {
                name: 'ApplicationInsights__ConnectionString'
                value: appInsightsConnectionString
              }
            ],
            app.env
          )
        }
      ]
      ingressTargetPort: app.disableIngress ? null : 8080
      disableIngress: app.disableIngress
      activeRevisionsMode: app.activeRevisionsMode
      environmentResourceId: environmentResourceId
      scaleSettings: {
          minReplicas: app.minReplicas
          maxReplicas: app.maxReplicas
      }
    }
  }
]

@description('System assigned managed identity principal ids, in the same order as containerApps')
output systemAssignedMIPrincipalIds string[] = [
  for (app, i) in containerApps: containerApp[i].outputs.systemAssignedMIPrincipalId
]
