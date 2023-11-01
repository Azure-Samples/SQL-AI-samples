"""
This file do the setup for testing the flow locally.

Copyright (c) Microsoft Corporation.  All rights reserved.
"""
# %%
from promptflow import PFClient
from promptflow.entities import AzureOpenAIConnection, CustomConnection
import json
import os
import yaml
import subprocess

from azure.identity import DefaultAzureCredential
from azure.keyvault.secrets import SecretClient

# %%
# Keyvault Setup


def get_keyvault_secret(keyvault_uri: str, secret_name: str):
    """Use the default credential (e.g., az login) to get key vault access and retrieve a secret."""
    credential = DefaultAzureCredential(
        exclude_shared_token_cache_credential=True, exclude_visual_studio_credential=True)
    client = SecretClient(vault_url=keyvault_uri, credential=credential)
    secret = client.get_secret(secret_name)
    return secret.value


def upload_secret(vault_url, key, value):
    """Use the default credential (e.g., az login) to get key vault access and set a secret."""

    # changing key to lowercase and replacing underscores with dashes
    key = key.lower().replace('_', '-')

    # Use the default credential (e.g., az login)
    credential = DefaultAzureCredential(
        exclude_shared_token_cache_credential=True, exclude_visual_studio_credential=True)
    client = SecretClient(vault_url=vault_url, credential=credential)

    # Set (or update) the secret
    client.set_secret(key, value)


# Load the config json files
print("Loading flow config files...")
with open('./configs/flow_config.json') as f:
    config = json.load(f)

print("Loading keys from environment variables...")
with open('./configs/key_config_local.json') as f:
    keys = json.load(f)


# Send config_local.json contents to key vault
print('Uploading all secrets from key_config_local.json to Key Vault')
print('Note: keys will be converted to lowercase and underscores will be replaced with dashes (Key Vault requirement)')
for key, value in keys.items():
    print(f'Uploading secret {key}')
    upload_secret(config['keyvault_uri'], key, value)
    print(f'Secret {key} uploaded successfully')

# %%
# setting up AOAI connection
print("Setting up AOAI connections.")
print('Getting AOAI API key from keyvault')
aoai_api_key = get_keyvault_secret(config['keyvault_uri'], 'aoai-api-key')

pf = PFClient()

conn_name = config['azure_open_ai_connection_name']
connection = AzureOpenAIConnection(
    name=conn_name,
    api_key=aoai_api_key,
    api_base=config["aoai_api_base"],
    api_type=config["aoai_api_type"],
    api_version=config["aoai_api_version"],
)
conn = pf.connections.create_or_update(connection)
print("successfully created connection")
# %%
# setting up SQL connection
print("Setting up SQL connections.")
print('Getting SQL Connection STRING from keyvault')
sqlConnectionString = get_keyvault_secret(
    config['keyvault_uri'], 'connectionString')
print('Getting Azure Congnitive Search Key from keyvault')
acs_key = get_keyvault_secret(config['keyvault_uri'], 'acs-key')
print('Getting opeenai embedding key from keyvault')
aoai_api_key_embed = get_keyvault_secret(
    config['keyvault_uri'], 'aoai-api-key-embed')

conn_name = config['SQLDB_connection_name']
connection = CustomConnection(
    name=conn_name,
    secrets={"connectionString": sqlConnectionString, "search-key": acs_key, "OPENAI_API_BASE_EMBED":
             config["OPENAI_API_BASE_EMBED"], "OPENAI_API_VERSION": config["OPENAI_API_VERSION"], "OPENAI_API_KEY_EMBED": aoai_api_key_embed}
)
conn = pf.connections.create_or_update(connection)
print("successfully created connection")

# %%
# Load the yaml file to dictionary path is azure_openai.yml
print("Setting up flow.dag.yaml.")
with open('./promptflow/flow.dag.sample.yaml') as f:
    config_flow = yaml.load(f, Loader=yaml.FullLoader)
# import pdb; pdb.set_trace()
# replace the deployment_name with the one you want to use
for node in config_flow["nodes"]:
    # Setting up model for the agent chat node.
    if node.get('api') == "chat":
        if 'deployment_name' in node:
            node["deployment_name"] = config['aoai_deployment_name']
        if 'deployment_name' in node['inputs']:
            node['inputs']['deployment_name'] = config['aoai_deployment_name']
        # Setting up model for the final chat node.
        if 'connection' in node:
            node['connection'] = config['azure_open_ai_connection_name']
    else:
        if 'conn' in node['inputs']:
            node['inputs']['conn'] = config['SQLDB_connection_name']

# write the yaml file back
with open('./promptflow/flow.dag.yaml', 'w') as f:
    yaml.dump(config_flow, f)

# %%
