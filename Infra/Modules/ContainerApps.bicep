@description('Container App Name')
param containerAppsName string

@description('Environment resouce environment')
param environmentResourceId string

@description('Workspace Resource Id')
param workspaceResourceId string

@description('Container Registry Resource ID')
param containerRegsistryResourceId string

@description('App Insights Connection String')
param appInsightsConnectionString string

@description('Whether to disable all ingress traffic for this container app (e.g. worker services with no HTTP endpoint)')
param disableIngress bool = false

module containerApp 'br/public:avm/res/app/container-app:0.23.0' = {
  name: '${containerAppsName}-${uniqueString(deployment().name)}'
  params: {
    name: containerAppsName
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
        name: containerAppsName
        resources: {
          cpu: 1
          memory: '2.0Gi'
        }
        env: [
          {
            name: 'ApplicationInsights__ConnectionString'
            value: appInsightsConnectionString
          }
        ]
      }
    ]
    ingressTargetPort: disableIngress ? null : 8080
    disableIngress: disableIngress
    activeRevisionsMode: 'Multiple'
    environmentResourceId: environmentResourceId
  }
}

//ACR pull role assignment from container registry
module iams 'roleAssignments.bicep' = {
  name: '${containerAppsName}-Role-${uniqueString(deployment().name)}'
  params: {
    principalId: containerApp.outputs.systemAssignedMIPrincipalId
    roleDefinitionId: '7f951dda-4ed3-4680-a7ca-43fe172d538d'
    scopeResourceId: containerRegsistryResourceId
  }
}

output systemAssignedMIPrincipalId string = containerApp.outputs.systemAssignedMIPrincipalId
