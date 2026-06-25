Create identity 
```bash
az ad sp create-for-rbac --name deployment-credentials  --json-auth --output json
az role assignment create --role contributor --scopes /subscriptions/15cec3e2-1cd9-4276-b215-5370e2425b38/resourceGroups/MassTransit-Playground
```
Set the output JSON AZURE_CREDENTIALS in GitHub action secrets 

```bash
az containerapp registry set --name art-auction-api --resource-group MassTransit-Playground --server masstransitcontainerregistry.azurecr.io --identity system
```
