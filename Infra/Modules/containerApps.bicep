import { containerAppConfig } from '../Types/types.bicep'

@description('Container Apps to deploy')
param containerApps containerAppConfig[]

@description('Environment resouce environment')
param environmentResourceId string

@description('Workspace Resource Id')
param workspaceResourceId string

@description('App Insights Connection String')
param appInsightsConnectionString string

@description('Service Bus namespace name, used as the namespace for azure-servicebus scale rules')
param serviceBusName string

@description('Container Registry name, used so apps can pull their images using their own user-assigned identity')
param acrName string

@description('Map of container app name to the image it is currently running. Apps missing from this map (e.g. on first deploy) fall back to the placeholder image.')
param imageOverrides object = {}

@description('User-assigned managed identity resource IDs, in the same order as containerApps. Granted AcrPull/Service Bus roles ahead of time so revision provisioning never races role assignment.')
param identityResourceIds string[]

@description('User-assigned managed identity client IDs, in the same order as containerApps. Injected as ManagedIdentity__ClientId so each app pins DefaultAzureCredential to its own identity.')
param identityClientIds string[]

var placeholderImage = 'mcr.microsoft.com/k8se/quickstart:latest'

module containerApp 'br/public:avm/res/app/container-app:0.23.0' = [
  for (app, i) in containerApps: {
    name: '${app.name}-${uniqueString(deployment().name)}'
    params: {
      name: app.name
      managedIdentities: {
        userAssignedResourceIds: [
          identityResourceIds[i]
        ]
      }
      registries: [
        {
          server: '${acrName}.azurecr.io'
          identity: identityResourceIds[i]
        }
      ]
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
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: concat(
            [
              {
                name: 'ApplicationInsights__ConnectionString'
                value: appInsightsConnectionString
              }
              {
                name: 'ManagedIdentity__ClientId'
                value: identityClientIds[i]
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
          rules: [
            for queueName in app.scaleQueueNames: {
              name: 'sb-${toLower(replace(queueName, '_', '-'))}'
              custom: {
                type: 'azure-servicebus'
                identity: identityResourceIds[i]
                metadata: {
                  queueName: queueName
                  namespace: serviceBusName
                  messageCount: '1'
                }
              }
            }
          ]
      }
    }
  }
]
