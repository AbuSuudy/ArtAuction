@description('Container App Name')
param containerAppsName string

@description('Environment resouce environment')
param environmentResourceId string

@description('Workspace Resource Id')
param workspaceResourceId string

@description('Container Registry Resource ID')
param containerRegsistryResourceId string

@description('Exposed port in container')
param exposedPort bool

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
    //Will deploy as part of my application pipeline
    containers:[]
    activeRevisionsMode: 'Multiple'
    environmentResourceId: environmentResourceId
    additionalPortMappings:  exposedPort  ? [
      {
        exposedPort: 8080
        external: false
        targetPort: 8080
      }
    ] : []
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
