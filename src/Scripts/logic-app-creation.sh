# Set the resource group and logic app name
resourceGroupName="myResourceGroup"
logicAppName="myLogicApp"

# Create the resource group
az group create --name $resourceGroupName --location eastus

# Create the logic app
az logicapp create --resource-group $resourceGroupName --name $logicAppName --location eastus

# Get the logic app ID
logicAppId=$(az logicapp show --resource-group $resourceGroupName --name $logicAppName --query id --output tsv)

# Set the trigger interval to 30 minutes
triggerInterval=30

# Create the trigger
az resource create --resource-group $resourceGroupName --resource-type "Microsoft.Logic/workflows/triggers" --parent $logicAppId --name "RecurrenceTrigger" --properties '{"recurrence":{"frequency":"Minute","interval":'$triggerInterval'}}'

# Get the trigger ID
triggerId=$(az resource show --resource-group $resourceGroupName --resource-type "Microsoft.Logic/workflows/triggers" --name "RecurrenceTrigger" --parent $logicAppId --query id --output tsv)

# Create the Outlook 365 connector
az logicapp connector create --resource-group $resourceGroupName --logic-app-name $logicAppName --connector-name "Outlook365" --parameters '{"TenantID":{"value":"<TENANT_ID>"}}'

# Get the Outlook 365 connector ID
outlookConnectorId=$(az logicapp connector show --resource-group $resourceGroupName --logic-app-name $logicAppName --connector-name "Outlook365" --query id --output tsv)

# Add the "Get email" action to the logic app
az resource create --resource-group $resourceGroupName --resource-type "Microsoft.Logic/workflows/actions" --parent $logicAppId --name "GetEmails" --properties '{"inputs":{"host":{"connection":{"name":"@parameters('$connector')['TenantID']"}},"method":"get","path":"/MailFolders/inbox/Messages","queries":{"filter":"receivedDateTime gt @{triggers().outputs.body.LastRunTime}"}}}' --parameters '{"$connector":{"referenceName":"Outlook365","id":"'$outlookConnectorId'"}}'

# Get the "Get email" action ID
getEmailActionId=$(az resource show --resource-group $resourceGroupName --resource-type "Microsoft.Logic/workflows/actions" --name "GetEmails" --parent $logicAppId --query id --output tsv)

# Add the HTTP POST connector
az logicapp connector create --resource-group $resourceGroupName --logic-app-name $logicAppName --connector-name "HTTP" --parameters '{"Method":{"value":"POST"},"Uri":{"value":"<POST_URI>"}}'

# Get the HTTP POST connector ID
httpConnectorId=$(az logicapp connector show --resource-group $resourceGroupName --logic-app-name $

# Add the "HTTP POST" action to the logic app
az resource create --resource-group $resourceGroupName --resource-type "Microsoft.Logic/workflows/actions" --parent $logicAppId --name "SendEmails" --properties '{"inputs":{"host":{"connection":{"name":"@parameters('$connector')['Uri']"}},"method":"post","body":"@{body('GetEmails')}"}}' --parameters '{"$connector":{"referenceName":"HTTP","id":"'$httpConnectorId'"}}'

# Set the trigger for the "Get email" action to be the recurrence trigger
az resource update --resource-group $resourceGroupName --resource-type "Microsoft.Logic/workflows/actions" --name "GetEmails" --parent $logicAppId --set properties.triggers='[{"name":"RecurrenceTrigger","type":"Recurrence","recurrence":{"frequency":"Minute","interval":'$triggerInterval'}}]'

# Enable the logic app
az logicapp start --resource-group $resourceGroupName --name $logicAppName

