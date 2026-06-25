```bash
az ad sp create-for-rbac --name deployment-credentials --role contributor --scopes /subscriptions/15cec3e2-1cd9-4276-b215-5370e2425b38/resourceGroups/MassTransit-Playground  --json-auth --output json
```

AZURE_CREDENTIALS = output

```bash
az containerapp registry set --name art-auction-api --resource-group MassTransit-Playground --server masstransitcontainerregistry.azurecr.io --identity system
```
