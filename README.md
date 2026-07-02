Create identity used for deployment and sett the output JSON AZURE_CREDENTIALS in GitHub action secrets 
```bash
az ad sp create-for-rbac --name deployment-credentials  --json-auth --output json
```
Assign that identity to be a owner at the subscription level since it will be used to create: Resouce Group, Resources in the infra pipeline.
```
az role assignment create --assignee-object-id 04f2c0c7-8d8c-4b53-a66d-b94ba77a881f --assignee-principal-type ServicePrincipal --role "Owner" --scope /subscriptions/15cec3e2-1cd9-4276-b215-5370e2425b38
```
Allow the container app to pull from the container registry
```bash
az containerapp registry set --name art-auction-api --resource-group MassTransit-Playground --server masstransitcontainerregistry.azurecr.io --identity system
```

## Delete Resources From Deployment Stack 

```
Remove-AzSubscriptionDeploymentStack -Name "Development" -ActionOnUnmanage DeleteAll -Force 
```
