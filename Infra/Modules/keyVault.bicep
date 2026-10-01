@description('Key Vault Name')
param keyVaultName string

module keyVault 'br/public:avm/res/key-vault/vault:0.14.2' = {
  name: '${keyVaultName}-${uniqueString(resourceGroup().id, keyVaultName)}'
  params: {
    name: keyVaultName
    sku: 'standard'
  }
}

output resourceId string = keyVault.outputs.resourceId
