## Setup
Create identity used for deployment and sett the output JSON AZURE_CREDENTIALS in GitHub action secrets 
```bash
az ad sp create-for-rbac --name deployment-credentials  --json-auth --output json
```
Assign that identity to be an owner at the subscription level since it will be used to create: Resouce Group, Resources in the infra pipeline.
```
az role assignment create --assignee-object-id 04f2c0c7-8d8c-4b53-a66d-b94ba77a881f --assignee-principal-type ServicePrincipal --role "Owner" --scope /subscriptions/15cec3e2-1cd9-4276-b215-5370e2425b38
```

Assign as deployment stack contributor 
```
az role assignment create --assignee 04f2c0c7-8d8c-4b53-a66d-b94ba77a881f --role bf7f8882-3383-422a-806a-6526c631a88a --scope /subscriptions/15cec3e2-1cd9-4276-b215-5370e2425b38
```

## Container Registry
Allow the container app to pull from the container registry. This is done in my infra pipeline, but worth noting for the future. I was having issues setting this in the registry property in the AVM for the container.
```bash
az containerapp registry set --name art-auction-api --resource-group MassTransit-Playground --server masstransitcontainerregistry.azurecr.io --identity system
```

## Delete Resources From Deployment Stack 

```
Remove-AzSubscriptionDeploymentStack -Name "Development" -ActionOnUnmanage DeleteAll -Force 
```

## Generate Token from Identity Provider using PowerShell

```
$body = @{
    grant_type    = "client_credentials"
    client_id     = "ebb17797-97dd-4639-99d4-071a33245217"
    client_secret = ""
    scope         = "api://d49faf26-5082-4caa-a823-997ad1337897/.default"
}

$response = Invoke-RestMethod -Uri "https://login.microsoftonline.com/3853050a-07fd-4295-9fec-e56ecda64939/oauth2/v2.0/token" -Method Post -Body $body

$response.access_token
```
