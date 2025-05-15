@description('Location for all resources')
param location string = resourceGroup().location

@description('Prefix for all resource names.')
param appNamePrefix string = 'twlocr'

@description('Suffix to ensure unique resource names')
param uniqueSuffix string = substring(uniqueString(resourceGroup().id), 0, 4) 

@description('Unique Prefix for all resource names.')
param uniqueAppNamePrefix string = '${appNamePrefix}${uniqueSuffix}'

// Azure Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${uniqueAppNamePrefix}-kv'
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: [

    ] // Add access policies as needed
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'None'
      ipRules: []
      virtualNetworkRules: []
    }
  }
}

// Azure Web PubSub Service
resource webPubSub 'Microsoft.SignalRService/webPubSub@2024-10-01-preview' = {
  name: '${uniqueAppNamePrefix}-webpubsub'
  location: location
  sku: {
    name: 'Premium_P1'
    tier: 'Premium'
    capacity: 1
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    networkACLs: {
      defaultAction: 'Deny'
      publicNetwork: {
        allow: [
          'ServerConnection'
          'ClientConnection'
          'RESTAPI'
          'Trace'
        ]
      }
    }
    disableAadAuth: true
  }

  resource webPubSubHub 'hubs@2024-10-01-preview' = {
    name: 'cr'
    properties: {
      eventHandlers: [
        {
          urlTemplate: 'https://${appNamePrefix}-${uniqueSuffix}.azurewebsites.net/runtime/webhooks/webpubsub?code='
          userEventPattern: '*'
          auth: {
            type: 'none'
          }
        }
      ]
    }
  }
}

// Storage Account
resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: uniqueAppNamePrefix
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    defaultToOAuthAuthentication: true
    publicNetworkAccess: 'Enabled'
    allowCrossTenantReplication: false
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    networkAcls: {
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
      defaultAction: 'Allow'
    }
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        blob: {
          enabled: true
          keyType: 'Account'
        }
        file: {
          enabled: true
          keyType: 'Account'
        }
        queue: {
          enabled: true
          keyType: 'Service'
        }
        table: {
          enabled: true
          keyType: 'Service'
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

resource storageBlobs 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
  parent: storage
  name: 'default'
  properties: {}
}

resource storageBlobsContainers 'Microsoft.Storage/storageAccounts/blobServices/containers@2024-01-01' = {
  parent: storageBlobs
  name: '${uniqueAppNamePrefix}-storage-blob'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

// Azure Function App Service Plan
resource functionPlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${uniqueAppNamePrefix}-func-plan'
  location: location
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
    size: 'FC1'
    family: 'FC'
    capacity: 0
  }
  kind: 'functionapp'
  properties: {
    perSiteScaling: false
    elasticScaleEnabled: false
    maximumElasticWorkerCount: 1
    isSpot: false
    reserved: true
    isXenon: false
    hyperV: false
    targetWorkerCount: 0
    targetWorkerSizeId: 0
    zoneRedundant: false
  }
}

// Azure Function App
resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: uniqueAppNamePrefix
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: functionPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storage.properties.primaryEndpoints.blob
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
      ]
    }
    functionAppConfig: {
      deployment:{
        storage: {
          type: 'blobcontainer'
          value: 'https://${uniqueAppNamePrefix}.blob.${environment().suffixes.storage}/${uniqueAppNamePrefix}-storage-blob'
          authentication: {
            type: 'storageaccountconnectionstring'
            storageAccountConnectionStringName: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
          }
        }
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '9.0'
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 40
        instanceMemoryMB: 4096
      }
    }
    httpsOnly: true
  }
}

// deploy a model
resource csAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: '${uniqueAppNamePrefix}-ai'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'S0'
  }
  kind: 'AIServices'
  properties: {
    customSubDomainName: uniqueAppNamePrefix
    publicNetworkAccess: 'Enabled'
  }

  resource csAccountDeployment 'deployments@2025-04-01-preview' = {
    name: 'gpt-4o'
    sku: {
      name: 'GlobalStandard'
      capacity: 250
    }
    properties: {
      model: {
        format: 'OpenAI'
        name: 'gpt-4o'
        version: '2024-11-20'
      }
      versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
      currentCapacity: 250
      raiPolicyName: 'Microsoft.DefaultV2'
    }
  }
}

// Azure Application Insights
resource aiAppInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${uniqueAppNamePrefix}-ai-insights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    Request_Source: 'rest'
    IngestionMode: 'ApplicationInsights'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Disabled'
    ForceCustomerStorageForProfiler: false
    ImmediatePurgeDataOn30Days: true
  }
}

// Azure Foundry AI Project
resource mlsWorkspace 'Microsoft.MachineLearningServices/workspaces@2025-01-01-preview' = {
  name: '${uniqueAppNamePrefix}-ai-project'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  kind: 'Project'
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    friendlyName: appNamePrefix
    v1LegacyMode: false
    hubResourceId: aiHub.id
  }

  resource aisConnection 'connections@2025-01-01-preview' = {
    name: '${uniqueAppNamePrefix}-ai-project-connection-ais'
    properties: {
      category: 'AIServices'
      target: csAccount.properties.endpoint
      authType: 'ApiKey'
      isSharedToAll: true
      credentials: {
        key: '${listKeys(csAccount.id, '2021-10-01').key1}'
      }
      metadata: {
        ApiType: 'Azure'
        ResourceId: csAccount.id
      }
    }
  }

  resource aoaiConnection 'connections@2025-01-01-preview' = {
    name: '${uniqueAppNamePrefix}-ai-project-connection-aoai'
    properties: {
      category: 'AzureOpenAI'
      target: 'https://${csAccount.name}.openai.azure.com/'
      authType: 'ApiKey'
      isSharedToAll: true
      credentials: {
        key: '${listKeys(csAccount.id, '2021-10-01').key1}'
      }
      metadata: {
        ApiType: 'Azure'
        ResourceId: csAccount.id
      }
    }
  }
}

// Azure Foundry AI Hub
resource aiHub 'Microsoft.MachineLearningServices/workspaces@2025-01-01-preview' = {
  name: '${uniqueAppNamePrefix}-ai-hub'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {

    // dependent resources
    keyVault: keyVault.id
    storageAccount: storage.id
    applicationInsights: aiAppInsights.id
  }
  kind: 'hub'
}
