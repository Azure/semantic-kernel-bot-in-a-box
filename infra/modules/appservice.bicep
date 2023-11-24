param resourceLocation string
param prefix string
param msiID string
param msiClientID string
param sku string = 'S1'
param tags object = {}
param openaiGPTModel string
param openaiEmbeddingsModel string

param openaiEndpoint string
param searchEndpoint string
param documentIntelligenceEndpoint string
param sqlConnectionString string
param cosmosEndpoint string

var uniqueSuffix = substring(uniqueString(subscription().id, resourceGroup().id), 1, 3)
var appServicePlanName = '${prefix}-plan-${uniqueSuffix}'
var appServiceName = '${prefix}-app-${uniqueSuffix}'

resource appServicePlan 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: appServicePlanName
  location: resourceLocation
  tags: tags
  sku: {
    name: sku
  }
}

resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceName
  location: resourceLocation
  tags: tags
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
          name: 'SQL_CONNECTION_STRING'
          value: sqlConnectionString
        }
        {
          name: 'COSMOS_API_ENDPOINT'
          value: cosmosEndpoint
        }
      ]
    }
  }
}

output hostName string = appService.properties.defaultHostName
