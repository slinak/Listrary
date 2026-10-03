// Provisions the cheapest App Service (F1 Free, Linux) to host ListManager.
// Deploy once via: az deployment group create -g <resource-group> -f infra/main.bicep -p appName=<name> sqlConnectionString=<conn-string>
param appName string
param location string = resourceGroup().location

@secure()
param sqlConnectionString string

var planName = '${appName}-plan'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  kind: 'linux'
  sku: {
    name: 'F1'
    tier: 'Free'
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: false // not supported on the Free tier
      ftpsState: 'FtpsOnly'
      connectionStrings: [
        {
          name: 'DefaultConnection'
          connectionString: sqlConnectionString
          type: 'SQLAzure'
        }
      ]
    }
  }
}

output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
