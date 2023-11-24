param resourceLocation string
param prefix string
param tags object = {}

param msiPrincipalID string

var uniqueSuffix = substring(uniqueString(subscription().id, resourceGroup().id), 1, 3) 
var cosmosAccountName = '${prefix}-cosmos-${uniqueSuffix}'


resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2022-05-15' =  {
  name: cosmosAccountName
  location: resourceLocation
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    locations: [
      {
        locationName: resourceLocation
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    databaseAccountOfferType: 'Standard'
  }

  resource db 'sqlDatabases' = {
    name: 'SemanticKernelBot'
    properties: {
      resource: {
        id: 'SemanticKernelBot'
      }
    }



  resource col 'containers' = {
    name: 'Conversations'
    properties: {
      resource: {
        id: 'Conversations'
        partitionKey: {
          paths: ['/id']
          kind: 'Hash'
        }
      }
    }
  }
  }
}

resource cosmosDataReader 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2021-10-15' existing = {
  name: '00000000-0000-0000-0000-000000000001'
  parent: cosmosAccount
}

resource cosmosDataContributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2021-10-15' existing = {
  name: '00000000-0000-0000-0000-000000000002'
  parent: cosmosAccount
}

resource appReadAccess 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid(cosmosAccount.id, msiPrincipalID, cosmosDataReader.id)
  parent: cosmosAccount
  properties: {
    roleDefinitionId: cosmosDataReader.id
    principalId: msiPrincipalID
    scope: cosmosAccount.id
  }
}

resource appWriteAccess 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid(cosmosAccount.id, msiPrincipalID, cosmosDataContributor.id)
  parent: cosmosAccount
  properties: {
    roleDefinitionId: cosmosDataContributor.id
    principalId: msiPrincipalID
    scope: cosmosAccount.id
  }
}



output cosmosAccountID string = cosmosAccount.id
output cosmosEndpoint string = cosmosAccount.properties.documentEndpoint
