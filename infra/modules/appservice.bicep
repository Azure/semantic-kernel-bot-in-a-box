param location string
param appServicePlanName string
param appServiceName string
param msiID string
param msiClientID string
param sku string = 'S1'
param tags object = {}
param openaiGPTModel string
param openaiEmbeddingsModel string

param documentIntelligenceName string
param bingName string

param openaiEndpoint string
param searchEndpoint string
param documentIntelligenceEndpoint string
param sqlConnectionString string
param cosmosEndpoint string

resource bing 'Microsoft.Bing/accounts@2020-06-10' existing = if (!empty(bingName)) {
  name: bingName
}

resource documentIntelligence 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = if (!empty(documentIntelligenceName)) {
  name: documentIntelligenceName
}

resource appServicePlan 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: sku
  }
}

resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceName
  location: location
  tags: union(tags, { 'azd-service-name': 'semantic-kernel-bot-app' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${msiID}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      http20Enabled: true
      appSettings: [
        {
          name: 'MicrosoftAppType'
          value: 'UserAssignedMSI'
        }
        {
          name: 'MicrosoftAppId'
          value: msiClientID
        }
        {
          name: 'MicrosoftAppTenantId'
          value: tenant().tenantId
        }
        {
          name: 'AOAI_API_ENDPOINT'
          value: openaiEndpoint
        }
        {
          name: 'AOAI_GPT_MODEL'
          value: openaiGPTModel
        }
        {
          name: 'AOAI_EMBEDDINGS_MODEL'
          value: openaiEmbeddingsModel
        }
        {
          name: 'SEARCH_API_ENDPOINT'
          value: searchEndpoint
        }
        {
          name: 'SEARCH_INDEX'
          value: 'hotels-sample-index'
        }
        {
          name: 'DOCINTEL_API_ENDPOINT'
          value: documentIntelligenceEndpoint
        }
        {
          name: 'DOCINTEL_API_KEY'
          value: documentIntelligence.listKeys().key1
        }
        {
          name: 'SQL_CONNECTION_STRING'
          value: sqlConnectionString
        }
        {
          name: 'COSMOS_API_ENDPOINT'
          value: cosmosEndpoint
        }
        {
          name: 'DIRECT_LINE_SECRET'
          value: ''
        }
        {
          name: 'BING_API_ENDPOINT'
          value: bing.properties.endpoint
        }
        {
          name: 'BING_API_KEY'
          value: bing.listKeys().key1
        }
        {
          name: 'PROMPT_WELCOME_MESSAGE'
          value: 'Welcome to Semantic Kernel Bot in-a-box! Ask me anything to get started.'
        }
        {
          name: 'PROMPT_SYSTEM_MESSAGE'
          value: 'Answer the questions as accurately as possible using the provided functions.'
        }
        {
          name: 'PROMPT_SUGGESTED_QUESTIONS'
          value: '[]'
        }
      ]
    }
  }
}

output hostName string = appService.properties.defaultHostName
