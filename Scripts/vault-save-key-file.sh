#!/bin/bash

# Set variables
privateKeyFilePath="PATH_TO_PRIVATE_KEY_FILE"
keyVaultName="YOUR_KEY_VAULT_NAME"
secretName="PRIVATE_KEY_FILE_SECRET_NAME"

# Read the private key file
privateKeyFile=$(cat "$privateKeyFilePath")

# Convert the private key file to a string
privateKeyFileString=$(echo "$privateKeyFile" | tr -d '\n')

# Create a new secret in Azure Key Vault
az keyvault secret set --vault-name "$keyVaultName" --name "$secretName" --value "$privateKeyFileString"
