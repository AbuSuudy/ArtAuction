import { containerAppConfig } from '../Types/types.bicep'

@description('Container Apps to deploy')
param containerApps containerAppConfig[]

@description('Environment resouce environment')
param environmentResourceId string

@description('Workspace Resource Id')
param workspaceResourceId string

@description('Container Registry Resource ID')
param containerRegsistryResourceId string

@description('App Insights Connection String')
param appInsightsConnectionString string

@description('Service Bus namespace name')
param serviceBusName string

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
          image: 'mcr.microsoft.com/k8se/quickstart:latest'
          name: app.name
          resources: {
            cpu: 1
            memory: '2.0Gi'
          }
          env: [
            {
              name: 'ApplicationInsights__ConnectionString'
              value: appInsightsConnectionString
            }
            {
              name: 'OTEL_SERVICE_NAME'
              value: app.name
            }
            {
              name: 'ServiceBus__Host'
              value: 'sb://${serviceBusName}.servicebus.windows.net'
            }
          ]
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

//ACR pull role assignment from container registry
module iams 'roleAssignments.bicep' = [
  for (app, i) in containerApps: {
    name: '${app.name}-Role-${uniqueString(deployment().name)}'
    params: {
      principalId: containerApp[i].outputs.systemAssignedMIPrincipalId
      roleDefinitionId: '7f951dda-4ed3-4680-a7ca-43fe172d538d'
      scopeResourceId: containerRegsistryResourceId
    }
  }
]

@description('System assigned managed identity principal ids, in the same order as containerApps')
output systemAssignedMIPrincipalIds string[] = [
  for (app, i) in containerApps: containerApp[i].outputs.systemAssignedMIPrincipalId
]
