param location string
param adminLogin string
param adminSid string
param principalType string

var serverName = 'sql-calendar-sync-${location}'
var dbName = 'sqldb-calendar-sync-${location}'


resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: serverName
  location: location
  properties: {
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    restrictOutboundNetworkAccess: 'Disabled'
    administrators:{
      administratorType: 'ActiveDirectory'
      principalType: principalType
      login: adminLogin
      sid: adminSid
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

resource sqlerveAllowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties:{
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: dbName
  location: location
  sku: {
    name:'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Zone'
    isLedgerOn: false
    availabilityZone: 'NoPreference'
  }
}


output sqlDbConnString string = 'Server=tcp:${serverName}.database.windows.net,1433;Initial Catalog=${dbName};Authentication=Active Directory Default; Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
