@description('Storage Account')
param storageAccountName string

@description('Container Identity')
param containerIdentity string

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
    managedIdentities: {
      systemAssigned: true
    }
    //Storage Table Data Contributor
    roleAssignments: [
      {
        principalId: containerIdentity
        roleDefinitionIdOrName: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
      }
    ]
  }
}

output resourceId string = storageAccount.outputs.resourceId
