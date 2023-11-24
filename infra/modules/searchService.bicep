param resourceLocation string
param prefix string
param tags object = {}

param msiPrincipalID string

var uniqueSuffix = substring(uniqueString(subscription().id, resourceGroup().id), 1, 3) 
var searchAccountName = '${prefix}-search-${uniqueSuffix}'


resource searchAccount 'Microsoft.Search/searchServices@2020-08-01' = {
  name: searchAccountName
  location: resourceLocation
  tags: tags
  sku: {
    name: 'standard'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
  }
}

resource searchIndexContributor 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: '7ca78c08-252a-4471-8644-bb5ff32d4ba0'
}

resource appAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchAccount.id, msiPrincipalID, searchIndexContributor.id)
  scope: searchAccount
  properties: {
    roleDefinitionId: searchIndexContributor.id
    principalId: msiPrincipalID
    principalType: 'ServicePrincipal'
  }
}

output searchAccountID string = searchAccount.id
output searchEndpoint string = 'https://${searchAccount.name}.search.windows.net'
