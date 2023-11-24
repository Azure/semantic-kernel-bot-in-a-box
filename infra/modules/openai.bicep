param resourceLocation string
param prefix string
param tags object = {}

param gptModel string
param gptVersion string

param msiPrincipalID string

var uniqueSuffix = substring(uniqueString(subscription().id, resourceGroup().id), 1, 3)
var openaiAccountName = '${prefix}-openai-${uniqueSuffix}'


resource openaiAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openaiAccountName
  location: resourceLocation
  tags: tags
  sku: {
    name: 'S0'
  }
  kind: 'OpenAI'
  properties: {
    customSubDomainName: openaiAccountName
    apiProperties: {
      statisticsEnabled: false
    }
    networkAcls: {
      defaultAction: 'Allow'
    }
    publicNetworkAccess: 'Enabled'
  }
}

resource gpt4deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openaiAccount
  name: 'gpt-4'
  properties: {
    model: {
      format: 'OpenAI'
      name: gptModel
      version: gptVersion
    }
  }
  sku: {
    capacity: 10
    name: 'Standard'
  }
}


resource adaEmbeddingsdeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openaiAccount
  name: 'text-embedding-ada-002'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-ada-002'
      version: '2'
    }
  }
  sku: {
    capacity: 10
    name: 'Standard'
  }
  dependsOn: [gpt4deployment]
}

resource openaiUser 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
}

resource appAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openaiAccount.id, msiPrincipalID, openaiUser.id)
  scope: openaiAccount
  properties: {
    roleDefinitionId: openaiUser.id
    principalId: msiPrincipalID
    principalType: 'ServicePrincipal'
  }
}

output openaiAccountID string = openaiAccount.id
output openaiEndpoint string = openaiAccount.properties.endpoint
output openaiGPTModel string = gpt4deployment.name
output openaiEmbeddingsModel string = adaEmbeddingsdeployment.name
