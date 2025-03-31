# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

from promptflow import tool
import requests
import json
import os
import openai
import re
from promptflow.connections import CustomConnection, AzureOpenAIConnection
from azure.core.credentials import AzureKeyCredential


def generate_embeddings(text, conn: CustomConnection):
    openai.api_base = conn['OPENAI_API_BASE_EMBED']
    openai.api_type = "azure"
    # this may change in the future
    openai.api_version = conn['OPENAI_API_VERSION']
    openai.api_key = conn['OPENAI_API_KEY_EMBED']
    response = openai.Embedding.create(
        input=text, engine="text-embedding-ada-002")
    embeddings = response['data'][0]['embedding']
    return embeddings

# The inputs section will change based on the arguments of the tool function, after you save the code
# Adding type to arguments and return value will help the system show the types properly
# Please update the function name/signature per need


@tool
def get_products(search_text: str, conn: CustomConnection, top_k: int) -> str:
    search_service = "sqldricopilot"
    index_name = "promptflow-demo-product-description"
    search_key = conn["search-key"]
    api_version = "2023-07-01-Preview"

    headers = {
        'Content-Type': 'application/json',
        'api-key': search_key,
    }
    params = {
        'api-version': api_version,
    }
    body = {
        "vector": {
            "value": generate_embeddings(text=search_text, conn=conn),
            "fields": "DescriptionVector, ProductCategoryNameVector",
            "k": top_k
        },
        "search": search_text,
        "select": "ProductId, ProductCategoryName, Name, ProductNumber, Color, ListPrice, Size, ProductCategoryID, ProductModelID, ProductDescriptionID, Description",
        "top": top_k,
    }
    response = requests.post(
        f"https://{search_service}.search.windows.net/indexes/{index_name}/docs/search", headers=headers, params=params, json=body)
    response_json = response.json()['value']
    for i in range(len(response_json)):
        response_json[i]['ProductId'] = int(response_json[i]['ProductId'])

    return response_json
