param resourceLocation string
param prefix string
param tags object

param gptModel string = 'gpt-4'
param gptVersion string = '0613'

param deploySQL bool = true
param deploySearch bool = true
param deployDocIntel bool = true


module m_msi 'modules/msi.bicep' = {
  name: 'deploy_msi'
  params: {
    resourceLocation: resourceLocation
    prefix: prefix
    tags: tags
  }
}

module m_openai 'modules/openai.bicep' = {
  name: 'deploy_openai'
  params: {
    resourceLocation: resourceLocation
    gptModel: gptModel
    gptVersion: gptVersion
    msiPrincipalID: m_msi.outputs.msiPrincipalID
    prefix: prefix
    tags: tags
  }
}

module m_docs 'modules/documentIntelligence.bicep' = if (deployDocIntel) {
  name: 'deploy_docs'
  params: {
    resourceLocation: resourceLocation
    msiPrincipalID: m_msi.outputs.msiPrincipalID
    prefix: prefix
    tags: tags
  }
}

module m_search 'modules/searchService.bicep' = if (deploySearch) {
  name: 'deploy_search'
  params: {
    resourceLocation: resourceLocation
    msiPrincipalID: m_msi.outputs.msiPrincipalID
    prefix: prefix
    tags: tags
  }
}

module m_sql 'modules/sql.bicep' = if (deploySQL) {
  name: 'deploy_sql'
  params: {
    resourceLocation: resourceLocation
    msiPrincipalID: m_msi.outputs.msiPrincipalID
    prefix: prefix
    tags: tags
  }
}

module m_cosmos 'modules/cosmos.bicep' = {
  name: 'deploy_cosmos'
  params: {
    resourceLocation: resourceLocation
    msiPrincipalID: m_msi.outputs.msiPrincipalID
    prefix: prefix
    tags: tags
  }
}

module m_app 'modules/appservice.bicep' = {
  name: 'deploy_app'
  params: {
    resourceLocation: resourceLocation
    prefix: prefix
    tags: tags
    msiID: m_msi.outputs.msiID
    msiClientID: m_msi.outputs.msiClientID
    openaiEndpoint: m_openai.outputs.openaiEndpoint
    openaiGPTModel: m_openai.outputs.openaiGPTModel
    openaiEmbeddingsModel: m_openai.outputs.openaiEmbeddingsModel
    documentIntelligenceEndpoint: deployDocIntel ? m_docs.outputs.documentIntelligenceEndpoint : ''
    searchEndpoint: deploySearch ? m_search.outputs.searchEndpoint : ''
    cosmosEndpoint: m_cosmos.outputs.cosmosEndpoint
    sqlConnectionString: deploySQL ? m_sql.outputs.sqlConnectionString : ''
  }
  dependsOn: [
    m_openai, m_docs, m_cosmos, m_search, m_sql
  ]
}

module m_bot 'modules/botservice.bicep' = {
  name: 'deploy_bot'
  params: {
    resourceLocation: 'global'
    prefix: prefix
    tags: tags
    endpoint: 'https://${m_app.outputs.hostName}/api/messages'
    msiPrincipalID: m_msi.outputs.msiPrincipalID
    msiID: m_msi.outputs.msiID
  }
}
