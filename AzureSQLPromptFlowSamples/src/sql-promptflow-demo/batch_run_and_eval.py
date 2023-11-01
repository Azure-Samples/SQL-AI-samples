# %%
from azure.identity import DefaultAzureCredential, InteractiveBrowserCredential
# azure version promptflow apis
from promptflow.azure import PFClient
from promptflow.entities import AzureOpenAIConnection, CustomConnection
from promptflow.entities import Run
import json
import os
import yaml
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
# Set flow path and run input data

print("Batch run..")
flow_path = "./promptflow"
data_path = "./data/batch_run_data.jsonl"
# assume you have existing runtime with this name provisioned
runtime = config["promptflow_runtime"]

# create a run, stream it until it's finished
base_run = pf.run(
    flow=flow_path,
    data=data_path,
    runtime=runtime,
    stream=True,
    column_mapping={  # map the url field from the data to the url input of the flow
        "chat_history": "${data.chat_history}",
        "question": "${data.question}",
        "customer": "${data.customer}"
    }
)

details = pf.get_details(base_run)
pf.visualize(base_run)

# %%
# set eval flow path
eval_flow = "./evaluation"
data = "./data/batch_run_data.jsonl"

# set up eval flow
print("Setting up flow.dag.yaml.")
with open('./evaluation/flow.dag.sample.yaml') as f:
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
        if 'conn_db' in node['inputs']:
            node['inputs']['conn_db'] = config['SQLDB_connection_name']
        if 'conn' in node['inputs']:
            node['inputs']['conn'] = config['ACS_connection_name']

# write the yaml file back
with open('./evaluation/flow.dag.yaml', 'w') as f:
    yaml.dump(config_flow, f)
# %%
# run the flow with existing run
eval_run = pf.run(
    flow=eval_flow,
    data=data,
    run=base_run,
    runtime=runtime,
    column_mapping={  # map the url field from the data to the url input of the flow
        "question": "${data.question}",
        "answer": "${run.outputs.answer}",
        "context": "${run.outputs.retrieved_documents}"
    }
)

# stream the run until it's finished
pf.stream(eval_run)

# get the inputs/outputs details of a finished run.
details = pf.get_details(eval_run)
details.head(10)

# view the metrics of the eval run
metrics = pf.get_metrics(eval_run)
print(json.dumps(metrics, indent=4))

# visualize both the base run and the eval run
pf.visualize([base_run, eval_run])

# %%
