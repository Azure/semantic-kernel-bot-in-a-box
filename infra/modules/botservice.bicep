param resourceLocation string
param prefix string
param endpoint string
param msiID string
param msiPrincipalID string
param sku string = 'F0'
param kind string = 'azurebot'
param tags object = {}

var uniqueSuffix = substring(uniqueString(subscription().id, resourceGroup().id), 1, 3) 
var botServiceName = '${prefix}-bot-${uniqueSuffix}'

resource botservice 'Microsoft.BotService/botServices@2022-09-15' = {
  name: botServiceName
  location: resourceLocation
  tags: tags
  sku: {
    name: sku
  }
  kind: kind
  properties: {
    displayName: botServiceName
    endpoint: endpoint
    msaAppMSIResourceId: msiID
    msaAppId: msiPrincipalID
    msaAppType: 'UserAssignedMSI'
    msaAppTenantId: tenant().tenantId
  }
}
