@description('Container app names to create a user-assigned managed identity for, one each')
param containerAppNames string[]

resource identities 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = [
  for name in containerAppNames: {
    name: 'id-${name}'
    location: resourceGroup().location
  }
]

@description('Identity resource IDs, in the same order as containerAppNames')
output resourceIds string[] = [for i in range(0, length(containerAppNames)): identities[i].id]

@description('Identity principal IDs, in the same order as containerAppNames')
output principalIds string[] = [for i in range(0, length(containerAppNames)): identities[i].properties.principalId]
