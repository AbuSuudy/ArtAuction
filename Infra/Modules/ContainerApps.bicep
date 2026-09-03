@description('Container App Name')
param containerAppsName string

@description('Environment resouce environment')
param environmentResourceId string

@description('Workspace Resource Id')
param workspaceResourceId string

@description('Container Registry Resource ID')
param containerRegsistryResourceId string

module containerApp 'br/public:avm/res/app/container-app:0.22.1' = {
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
      }
    ]
    ingressTargetPort: 8080
    traffic: [
      {
        label: 'blue'
        revisionName: '${containerAppsName}-blue'
        weight: 100
      }
      {
        label: 'green'
        revisionName: '${containerAppsName}-green'
        weight: 0
      }
    ]
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
