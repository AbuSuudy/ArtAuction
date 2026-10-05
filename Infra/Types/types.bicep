@export()
type containerAppEnvVar = {
  name: string
  value: string
}

@export()
type containerAppConfig = {
  name: string
  disableIngress: bool
  activeRevisionsMode: 'Multiple' | 'Single'
  minReplicas: int
  maxReplicas: int
  @description('Container-specific environment variables, in addition to ApplicationInsights__ConnectionString which is always injected')
  env: containerAppEnvVar[]
}

@export()
@description('Access mode for a resource associated with the network security perimeter')
type nspResourceAccess = {
  name: string
  resourceType: 'Microsoft.KeyVault/vaults' | 'Microsoft.ServiceBus/namespaces' | 'Microsoft.OperationalInsights/workspaces' | 'Microsoft.Storage/storageAccounts'
  @description('Access mode for the association')
  accessMode: 'Learning' | 'Enforced' | 'Audit'
}
