```bash
az ad sp create-for-rbac --name my-app-credentials --role contributor --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/my-container-app-rg --json-auth --output json
```

AZURE_CREDENTIALS = output

