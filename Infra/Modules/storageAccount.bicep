@description('Storage Account')
param storageAccountName string

module storageAccount 'br/public:avm/res/storage/storage-account:0.32.1' = {
  name: '${storageAccountName}-${uniqueString(deployment().name)}'
  params: {
    name: storageAccountName
    kind: 'StorageV2'
    skuName: 'Standard_RAGRS'
    allowBlobPublicAccess: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

//Storage Table Data Contributor
module iams 'roleAssignments.bicep' = {
  name: '${storageAccountName}-Role-${uniqueString(deployment().name)}'
  params: {
    principalId: 'e9b6284d-9e6c-4e7e-af4c-369c64f12cad'
    roleDefinitionId: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
    scopeResourceId: storageAccount.outputs.resourceId
  }
}
