"""
Run the PromptFlow locally.

Copyright (c) Microsoft Corporation.  All rights reserved.
"""

# %%
from azure.identity import DefaultAzureCredential, InteractiveBrowserCredential
# azure version promptflow apis
from promptflow.azure import PFClient
from promptflow.entities import AzureOpenAIConnection, CustomConnection
from promptflow.entities import Run
import json
import os
from azure.core.credentials import TokenCredential

print("Loading configs from file.")
with open(f'configs/flow_config.json') as f:
    config = json.load(f)

# Getting credentials for local access
# -----------------------------------------------------------------------------

# %%
try:
    credential: TokenCredential = DefaultAzureCredential()
    # Check if given credential can get token successfully.
    credential.get_token("https://management.azure.com/.default")
except Exception:
    # Fall back to InteractiveBrowserCredential in case DefaultAzureCredential not work
    credential = InteractiveBrowserCredential()
# %%
# Get a handle to workspace
pf = PFClient(
    credential=credential,
    # this will look like xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
    subscription_id=config["subscription_id"],
    resource_group_name=config["resource_group_name"],
    workspace_name=config["workspace_name"],
)

print("currently Azure Promptflow SDK don't support create connection and upload to workspace, so we need to create connection manually IN PORTAL")
# Loading PromptFLow
# %%
# load flow
flow_path = "./promptflow"
data_path = "./promptflow/single_run_data.jsonl"
# assume you have existing runtime with this name provisioned
runtime = config["promptflow_runtime"]

# %%
# create run
base_run = pf.run(
    flow=flow_path,
    data=data_path,
    runtime=runtime,
    display_name="sql-promptflow-demo-1"
)
pf.stream(base_run)
# %%
details = pf.get_details(base_run)
details.head(10)

pf.visualize(base_run)
# %%
