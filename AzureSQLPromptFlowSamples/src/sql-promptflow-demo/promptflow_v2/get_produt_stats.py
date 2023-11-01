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
def get_sales_stat(products: list, sql_query_prep: dict, conn_db: CustomConnection):

  list_cate_id = list(map(lambda x: x['ProductCategoryID'], products))
  list_cate_id = str(tuple(list_cate_id)).replace(",)", ")")
  query_sales_stat = sql_query_prep['query_sales_stat'].replace("{list_cate}", list_cate_id)

  try:
      out_df = execute_sql(sql_query=query_sales_stat, conn_db=conn_db)
      out_json = out_df.to_json(orient="records")
      out_dict = json.loads(out_json)
  except:
      out_dict = {}

  return out_dict