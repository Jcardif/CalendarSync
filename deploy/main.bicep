param GoogleCalendarId string


var location = resourceGroup().location
var keyVaultName = 'kv-calendar-sync-${location}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' ={
  name: 'stcalendarsync${location}'
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties:{
    supportsHttpsTrafficOnly: true
    encryption:{
      services:{
        file:{
          keyType: 'Account'
          enabled: true
        }
        blob:{
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    accessTier: 'Hot'
  }
}

resource servicePlan 'Microsoft.Web/serverfarms@2023-12-01'= {
  name: 'asp-calendar-sync-${location}'
  location: location
  kind: 'functionapp'
  sku:{
    name: 'Y1'
  }
  properties:{}
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' ={
  name: 'func-calendar-sync-${location}'
  location: location
  kind: 'functionapp'
  properties:{
    serverFarmId: servicePlan.id
    siteConfig:{
      appSettings:[
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'KeyVaultName'
          value: keyVaultName
        }
      ]
    }
    httpsOnly: true
  }
  identity:{
    type: 'SystemAssigned'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2024-04-01-preview'={
  name: keyVaultName
  location: location
  properties:{
    tenantId: subscription().tenantId
    sku:{
      family: 'A'
      name: 'standard'
    }
    accessPolicies:[
      {
        tenantId: subscription().tenantId
        objectId: functionApp.identity.principalId
        permissions:{
          secrets: ['list','get']
          certificates: ['list','get']
          keys: ['list','get']          
        }
      }
    ]
  }
}

resource calendarIdSecret 'Microsoft.KeyVault/vaults/secrets@2024-04-01-preview'={
  name: 'GoogleCalendarId'
  parent: keyVault
  properties:{
    value: GoogleCalendarId
  }
}

module sql 'azure-sql.bicep' ={
  name: 'azureSql'
  params:{
    location: location
    adminLogin: functionApp.name
    adminSid: functionApp.identity.principalId
    principalType: 'Application'
  }
}


resource sqlDbConnString 'Microsoft.KeyVault/vaults/secrets@2024-04-01-preview'={
  name: 'SqlDbConnString'
  parent: keyVault
  properties:{
    value: sql.outputs.sqlDbConnString
  }
}
