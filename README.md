# CalendarSync
Sync Calendar Events/Meetings from work account with personal outlook leveraging Microsoft Graph, Google Calendar API &amp; Azure Functions.

# Getting Started
## App Settings
The local.settings.json has the following settings defined for use in the app.

```json

{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
  },

  "AzureAd":{
    "ClientId":"",
    "ClientSecret":"",
    "TenantId":"",
    "UserPrincipalName" : ""
  },

  "AzureKeyVault":
  {
    "SecretName":"",
    "KeyVaultUrl":""
  },

  "GoogleCloudConsole":{
    "CalendarId":""
  }

}

```

# Integration (Read/Write) with Google Calendar

To read and write events in Google Calendar without requiring user sign-in, you will need to use the Google Calendar API and authenticate your API requests using a service account.

A service account is a special kind of Google account that belongs to your application or a virtual machine (VM), instead of to an individual end user. 

Here is a general outline of the steps to follow before deploying the function:

- Go to the [Google API Console](https://console.cloud.google.com/).

- Create a new project or select an existing project.
Enable the Google Calendar API for your project. To do this, click the "Enable APIs and Services" button and search for "Google Calendar API". Click on the API, then click the "Enable" button.

- Create a service account and download the private key file. To do this, click the "Credentials" menu on the left, then click the "Create Credentials" button and select "Service Account". Follow the prompts to create a service account and download the private key file.

- Share the calendar with the email address of the service account. To do this, go to Google Calendar, click the "Settings" button, and then click the "Calendars" tab. Find the calendar you want to access, click the "Share this Calendar" button and add the email address of the service account as a "Person" with "Make changes to events" permissions.

- In your Azure Function, use the Google .NET Client Library to make API requests to the Google Calendar API. You will need to pass in the path to the private key file and the email address of the service account when creating the CalendarService object.

- We'll use the Azure Key Vault to save the private key file.

- Go to the [Azure portal](https://portal.azure.com/) and create a new Key Vault or select an existing one.

- Run [the Script](/Scripts/vault-save-key-file.sh) to save the downladed Private key file to azure vault. Uodate the script variables with values from azure.

# Configuring Azure Functions &amp; Managed Identity to access Key Vault
- After publishing the Azure Function App to Azure we'll need to authenticate it with the Key Vault using system-assigned managed identity.





