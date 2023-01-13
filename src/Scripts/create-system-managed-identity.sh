## Create system-assigned managed identity
az functionapp identity assign --name functionappname --resource-group myResourceGroup

## Output of above command is as below.
## {
##    "principalId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
##    "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
##    "type": "SystemAssigned"
## }
## 
## principalId is required for setting access policy through CLI