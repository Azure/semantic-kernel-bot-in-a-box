using 'main.bicep'

param resourceLocation = 'eastus2'
param prefix = 'aitoolkit'

param gptModel = 'gpt-4'
param gptVersion = '1106-Preview'

param tags = {
  Owner: 'AI Team'
  Project: 'GPTBot'
  Environment: 'Dev'
  Toolkit: 'Bicep'
}

param deployDocIntel = true
param deploySearch = true
param deploySQL = true
