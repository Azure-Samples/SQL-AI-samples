from promptflow import tool
from promptflow.connections import CustomConnection
from azure.core.credentials import AzureKeyCredential
import requests
import json
import os
import openai
import re
import pyodbc
import pandas as pd
import numpy as np
import sqlalchemy as sa


def generate_embeddings(text, conn: CustomConnection):
    openai.api_base = conn.OPENAI_API_BASE_EMBED
    openai.api_key = conn.OPENAI_API_KEY_EMBED
    openai.api_version = conn.OPENAI_API_VERSION
    openai.api_type = "azure"

    response = openai.Embedding.create(
        input=text, engine="text-embedding-ada-002")
    embeddings = response['data'][0]['embedding']
    return embeddings

def execute_sql(sql_query: str, conn_db: CustomConnection):

    conn_string = conn_db['connection-string']
    with pyodbc.connect(conn_string,autocommit=True) as conn:
        with conn.cursor() as cursor:
            cursor.execute(sql_query)
            query_out = cursor.fetchall()

    toReturn = pd.DataFrame((tuple(t) for t in query_out)) 
    toReturn.columns = [column[0] for column in cursor.description]

    return toReturn

@tool
def get_product(search_text: str, sql_query_prep: dict, conn: CustomConnection, conn_db: CustomConnection, top_k:int) -> str:
    search_service = "sqldricopilot"
    index_name =  "promptflow-demo-product-description"
    search_key = conn["acs-search-key"]
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
            "value": generate_embeddings(text = search_text, conn = conn),
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

    list_prod_id = list(map(lambda x: x['ProductId'], response_json))
    list_prod_id = str(tuple(list_prod_id)).replace(",)", ")")
    query_product = sql_query_prep['query_prod_byID'].replace("{list_product}", list_prod_id)

    try:
        out_df = execute_sql(sql_query=query_product, conn_db=conn_db)
        out_json = out_df.to_json(orient="records")
        out_dict = json.loads(out_json)
    except:
        out_dict = {}

    return out_dict