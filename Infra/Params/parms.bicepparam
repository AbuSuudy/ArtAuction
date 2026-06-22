using '../Main.bicep'

param logAnalyticsName = 'log-analytics-001'
param appInsightsName = 'app-insight-001'
param acrName = 'masstransitcontainerregistry'
param environmentName = 'container-environment-001'
param resourceGroupName = 'MassTransit-Playground'
param serviceBusName = 'sb-masstransit-neu-002'
param storageAccountName = 'stmasstransit001'


//az deployment sub create --name ExampleDeployment --location uksouth --template-file main.bicep --parameters Infra\Params\parms.bicepparam
