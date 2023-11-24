param resourceLocation string
param prefix string
param tags object = {}

var uniqueSuffix = substring(uniqueString(subscription().id, resourceGroup().id), 1, 3) 
var msiName = '${prefix}-id-${uniqueSuffix}'

resource msi 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: msiName
  location: resourceLocation
  tags: tags
}

output msiID string = msi.id
output msiClientID string = msi.properties.clientId
output msiPrincipalID string = msi.properties.principalId
