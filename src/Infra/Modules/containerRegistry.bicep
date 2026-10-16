@description('The name of the Azure Container Registry')
param acrName string

module avmContainerRegistry 'br/public:avm/res/container-registry/registry:0.12.1' = {
  name: '${acrName}-${uniqueString(deployment().name)}'
  params: {
    name: acrName
    acrSku: 'Standard'
  }
}

output containerRegistryResourceId string = avmContainerRegistry.outputs.resourceId
