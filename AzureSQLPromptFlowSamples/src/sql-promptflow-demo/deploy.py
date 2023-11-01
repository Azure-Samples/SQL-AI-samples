"""
Deploying this prompt flow to an endpoint.

Copyright (c) Microsoft Corporation.  All rights reserved.

"""

# Imports and loading config file
# -----------------------------------------------------------------------------


# %%
import json
import yaml
import os
import shutil
import subprocess

print("Loading configs from file.")
with open(f'./configs/flow_config.json') as f:
    config = json.load(f)

# Creating helper functions
# -----------------------------------------------------------------------------


# %%
def run_command(command: list, verbose=True):
    """Executing a command as a bus-process."""
    process = subprocess.Popen(
        command, stdout=subprocess.PIPE, stderr=subprocess.PIPE, shell=True)
    stdout, stderr = process.communicate()
    if verbose:
        print(stdout.decode('utf-8'))
        print(stderr.decode('utf-8'))
    return stdout, stderr


def get_latest_model_version(model_name):
    """Getting the latest model version for a given model."""
    command = 'az ml model list --output json'.split()
    stdout, stderr = run_command(command, verbose=False)
    model_list = json.loads(stdout.decode('utf-8'))
    model_list = [model for model in model_list if model['name'] == model_name]
    version_list = [int(model['latest version']) for model in model_list]
    return max(version_list)


def check_if_deployment_exists(endpoint_name, endpoint_deployment_name):
    """Checks if a given deployment already exists for an endpoint."""
    command = f"az ml online-deployment list --endpoint-name {endpoint_name} --output json".split(
    )
    print(command)
    stdout, stderr = run_command(command, verbose=False)
    print(stdout.decode('utf-8'))
    if stdout is not None and stdout.decode('utf-8') != '':
        deployment_list = json.loads(stdout.decode('utf-8'))
        return len(deployment_list) > 0 and any(obj.get("name") == endpoint_deployment_name for obj in deployment_list)
    return False


#  Main Execution
# -----------------------------------------------------------------------------

# %%
# For the first time, install the azure-cli-ml extension
# -----------------------------------------------------------------------------
print("Install azure-cli-ml extension.")
command = ["az", "extension", "add", "-n", "ml"]
run_command(command)

# %%
# Set subscription id
# -----------------------------------------------------------------------------

print("Set azure subscription id.")
command = ["az", "account", "set", "--subscription", config["subscription_id"]]
run_command(command)

# %%
# Set workspace
# -----------------------------------------------------------------------------
print("Config workspace.")
command = ["az", "configure", "--defaults", f'workspace={config["workspace_name"]}',
           f'group={config["resource_group_name"]}']
run_command(command)

# %%
# Create endpoint using the endpoint.yaml. It will be generated from the template.
# -----------------------------------------------------------------------------
print("Setting up endpoint deployment file.")
with open('./deployment/endpoint_base.yaml') as f:
    config_flow = yaml.load(f, Loader=yaml.FullLoader)
# replace the deployment_name with the one you want to use
config_flow["name"] = config['endpoint_name']

# write the yaml file back
with open('./deployment/endpoint.yaml', 'w') as f:
    yaml.dump(config_flow, f)

print("Create endpoint if not exist.")
try:
    command = ["az", "ml", "online-endpoint",
               "create", "--file", "./deployment/endpoint.yaml"]
    run_command(command, verbose=False)
except Exception as e:
    command = ["az", "ml", "online-endpoint",
               "update", "--file", "./deployment/endpoint.yaml"]
    run_command(command)
finally:
    print("Endpoint created.")

# %%
# Register flow to a model in the AML workspace
# REMEMBER to:
# 1. Go to the azure portal for the workspace
# 2. add access control to the "data scientist role" for the endpoint for the workspace.
# -----------------------------------------------------------------------------
print("Setting up model registration file.")
with open('./deployment/model_base.yaml') as f:
    config_flow = yaml.load(f, Loader=yaml.FullLoader)
# replace the deployment_name with the one you want to use
config_flow["name"] = config['model_name']

# write the yaml file back
with open('./deployment/model.yaml', 'w') as f:
    yaml.dump(config_flow, f)

# %%
# Deploy model to AML workspace
# Before we upload model, we create tmp directory to store current solution
# We delete sensitive files (for now, config_local.json)
# -----------------------------------------------------------------------------
src_for_tmp = './promptflow_v2'
temp_dir = '../.copilot_tmp'

# if temp_dir exists already, delete it
if os.path.exists(temp_dir):
    shutil.rmtree(temp_dir)

print(f'Creating temp directory: {temp_dir}')
os.makedirs(temp_dir, exist_ok=True)

# %%
shutil.copytree(src_for_tmp, temp_dir, dirs_exist_ok=True)
# %%
# remove config_local.json from tmp folder
print('Removing config_local.json from tmp folder if it exists.')
# remove file if it exists
if os.path.exists(os.path.join(temp_dir, 'cofigs/key_config_local.json')):
    os.remove(os.path.join(temp_dir, 'cofigs/key_config_local.json'))
# %%
print("Register model to AML workspace.")
command = ["az", "ml", "model", "create", "--file", "./deployment/model.yaml"]
# Run the command and capture its outputs
run_command(command)
# %%
# now we delete tmp folder
print(f'Deleting temp directory: {temp_dir}')
shutil.rmtree(temp_dir)

# %%
# Grab latest model version from model registration step above
print('Getting latest model version...')
latest_model_version = get_latest_model_version(config['model_name'])
print(f'Latest model version: {latest_model_version}')

# %%
# Set up endpoint deployment
# -----------------------------------------------------------------------------
config_string = "deployment.subscription_id=<sub_id>,deployment.resource_group=<rg>,deployment.workspace_name=<ws>,deployment.endpoint_name=<ep_name>,deployment.deployment_name=<deployment_name>"
config_string = config_string.replace("<sub_id>", config["subscription_id"]).replace(
    "<rg>", config["resource_group_name"]).replace(
        "<ws>", config["workspace_name"]).replace(
            "<ep_name>", config["endpoint_name"]).replace(
                "<deployment_name>", config["endpoint_deployment_name"])

print("Setting up model deployment yaml.")
with open(r'./deployment/deployment_base.yaml') as f:
    config_flow = yaml.load(f, Loader=yaml.FullLoader)

# Replacing needed data in the deployment script.
config_flow["endpoint_name"] = config['endpoint_name']
config_flow["environment_variables"]["PRT_CONFIG_OVERRIDE"] = config_string
config_flow["name"] = config["endpoint_deployment_name"]
config_flow["environment"]["image"] = config["promptflow_image_base"]

model_name = config["model_name"]
config_flow["model"] = f"azureml:{model_name}:{latest_model_version}"

# write the yaml file back
with open(r'./deployment/deployment.yaml', 'w') as f:
    yaml.dump(config_flow, f)

# %%
print('Pushing online deployment.')

is_new_deployment = not check_if_deployment_exists(
    config['endpoint_name'], config["endpoint_deployment_name"])
if is_new_deployment:
    command = ["az", "ml", "online-deployment", "create", "--file",
               r"./deployment/deployment.yaml", "--all-traffic"]
    run_command(command)
else:
    # If run into existing deployment name, can update use the below
    command = ["az", "ml", "online-deployment", "update", "--file",
               r"./deployment/deployment.yaml"]
    run_command(command)

# %%
