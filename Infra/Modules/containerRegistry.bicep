@description('The name of the Azure Container Registry')
param acrName string

@description('Principal IDs to grant AcrPull on this registry')
param identites array = []

module avmContainerRegistry 'br/public:avm/res/container-registry/registry:0.12.1' = {
  name: '${acrName}-${uniqueString(deployment().name)}'
  params: {
    name: acrName
    acrSku: 'Standard'
    acrAdminUserEnabled : true
    //AcrPull, so the container apps can pull their images
    roleAssignments: [
      for identity in identites: {
        principalId: identity
        roleDefinitionIdOrName: '7f951dda-4ed3-4680-a7ca-43fe172d538d'
      }
    ]
  }
}

output containerRegistryResourceId string = avmContainerRegistry.outputs.resourceId
