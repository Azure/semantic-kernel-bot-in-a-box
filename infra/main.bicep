param resourceLocation string
param prefix string
param msaAppId string
@secure()
param msaAppPassword string
param tags object

var uniqueSuffix = substring(uniqueString(subscription().id, resourceGroup().id), 1, 3) 
var appServiceName = '${prefix}-app-${uniqueSuffix}'
var openaiAccountName = '${prefix}-openai-${uniqueSuffix}'
var searchAccountName = '${prefix}-search-${uniqueSuffix}'
var cosmosAccountName = '${prefix}-cosmos-${uniqueSuffix}'
var sqlServerName = '${prefix}-sql-${uniqueSuffix}'
var sqlDBName = '${prefix}-db-${uniqueSuffix}'


module m_openai 'modules/openai.bicep' = {
  name: 'deploy_openai'
  params: {
    resourceLocation: resourceLocation
    prefix: prefix
    tags: tags
  }
}

module m_search 'modules/searchService.bicep' = {
  name: 'deploy_search'
  params: {
    resourceLocation: resourceLocation
    prefix: prefix
    tags: tags
  }
}

module m_cosmos 'modules/cosmos.bicep' = {
  name: 'deploy_cosmos'
  params: {
    resourceLocation: resourceLocation
    prefix: prefix
    tags: tags
  }
}

module m_sql 'modules/sql.bicep' = {
  name: 'deploy_sql'
  params: {
    resourceLocation: resourceLocation
    prefix: prefix
    tags: tags
    sqlAdminLogin: msaAppId
    sqlAdminPassword: msaAppPassword
  }
}

module m_app 'modules/appservice.bicep' = {
  name: 'deploy_app'
  params: {
    resourceLocation: resourceLocation
    prefix: prefix
    tags: tags
    msaAppId: msaAppId
    msaAppPassword: msaAppPassword
    openaiAccountName: openaiAccountName
    searchAccountName: searchAccountName
    cosmosAccountName: cosmosAccountName
    sqlServerName: sqlServerName
    sqlDBName: sqlDBName
  }
  dependsOn: [
    m_openai, m_cosmos, m_search, m_sql
  ]
}

module m_bot 'modules/botservice.bicep' = {
  name: 'deploy_bot'
  params: {
    resourceLocation: 'global'
    prefix: prefix
    tags: tags
    endpoint: 'https://${m_app.outputs.hostName}/api/messages'
    msaAppId: msaAppId
  }
}

module m_rbac 'modules/rbac.bicep' = {
  name: 'deploy_rbac'
  params: {
    appServiceName: appServiceName
    openaiAccountName: openaiAccountName
    searchAccountName: searchAccountName
    cosmosAccountName: cosmosAccountName
  }
  dependsOn: [
    m_app, m_openai, m_cosmos, m_search, m_sql
  ]
}
