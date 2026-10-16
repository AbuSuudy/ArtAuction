@description('Storage Account')
param storageAccountName string

module storageAccount 'br/public:avm/res/storage/storage-account:0.32.1' = {
  name: '${storageAccountName}-${uniqueString(deployment().name)}'
  params: {
    name: storageAccountName
    kind: 'StorageV2'
    skuName: 'Standard_LRS'
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
    managedIdentities: {
      systemAssigned: true
    }
  }
}

output resourceId string = storageAccount.outputs.resourceId
