param resourceLocation string
param prefix string
param msaAppId string
param sku string = 'S1'
@secure()
param msaAppPassword string
param tags object = {}
param openaiAccountName string
param searchAccountName string
param cosmosAccountName string
param sqlServerName string
param sqlDBName string

var uniqueSuffix = substring(uniqueString(subscription().id, resourceGroup().id), 1, 3)
var appServicePlanName = '${prefix}-plan-${uniqueSuffix}'
var appServiceName = '${prefix}-app-${uniqueSuffix}'

resource openaiAccount 'Microsoft.CognitiveServices/accounts@2021-10-01' existing = {
  name: openaiAccountName
  resource gpt35deployment 'deployments' existing = {
    name: 'gpt-35-turbo'
  }
  resource gpt4deployment 'deployments' existing = {
    name: 'gpt-4'
  }
}

resource searchAccount 'Microsoft.Search/searchServices@2022-09-01' existing = {
  name: searchAccountName
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2022-05-15' existing = {
  name: cosmosAccountName
}

resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' existing = {
  name: sqlServerName
  resource sqlDB 'databases' existing = {
    name: sqlDBName
  }
}

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
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      http20Enabled: true
      appSettings: [
        {
          name: 'MicrosoftAppType'
          value: 'MultiTenant'
        }
        {
          name: 'MicrosoftAppId'
          value: msaAppId
        }
        {
          name: 'MicrosoftAppPassword'
          value: msaAppPassword
        }
        {
          name: 'AOAI_API_ENDPOINT'
          value: openaiAccount.properties.endpoint
        }
        {
          name: 'AOAI_MODEL'
          value: openaiAccount::gpt4deployment.name
        }
        {
          name: 'SEARCH_API_ENDPOINT'
          value: 'https://${searchAccount.name}.search.windows.net'
        }
        {
          name: 'SEARCH_INDEX'
          value: 'hotels-sample-index'
        }
        {
          name: 'SEARCH_API_KEY'
          value: searchAccount.listQueryKeys().value[0].key
        }
        {
          name: 'SQL_CONNECTION_STRING'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlServer::sqlDB.name};Persist Security Info=False;User ID=${sqlServer.properties.administratorLogin};Password=${msaAppPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
        }
        {
          name: 'COSMOS_ENDPOINT'
          value: cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString
        }
      ]
    }
  }
}

output hostName string = appService.properties.defaultHostName
