param resourceLocation string
param prefix string
param tags object = {}

param msiPrincipalID string

var uniqueSuffix = substring(uniqueString(subscription().id, resourceGroup().id), 1, 3)
var documentIntelligenceAccountName = '${prefix}-docs-${uniqueSuffix}'


resource documentIntelligenceAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: documentIntelligenceAccountName
  location: resourceLocation
  tags: tags
  sku: {
    name: 'S0'
  }
  kind: 'FormRecognizer'
  properties: {
    customSubDomainName: documentIntelligenceAccountName
    apiProperties: {
      statisticsEnabled: false
    }
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

resource contributor 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: 'b24988ac-6180-42a0-ab88-20f7382dd24c'
}

resource appAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(documentIntelligenceAccount.id, msiPrincipalID, contributor.id)
  scope: documentIntelligenceAccount
  properties: {
    roleDefinitionId: contributor.id
    principalId: msiPrincipalID
    principalType: 'ServicePrincipal'
  }
}

output documentIntelligenceAccountID string = documentIntelligenceAccount.id
output documentIntelligenceEndpoint string = documentIntelligenceAccount.properties.endpoint
