# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

from promptflow import tool
import pandas as pd
from promptflow.connections import CustomConnection 
import pyodbc
import json

@tool
def get_customer_details(inputs: dict, conn: CustomConnection):
    # this is a bug in promptflow where they treat this input type differently
    if type(inputs) == str:
       inputs_dict = eval(inputs)
    else:
       inputs_dict = inputs
    if inputs_dict['MiddleName'] == "":
      sqlQuery = f"""select * from [SalesLT].[Customer] WHERE FirstName='{inputs_dict['FirstName']}' and MiddleName is NULL and LastName='{inputs_dict['LastName']}'"""
    else: 
      sqlQuery = f"""select * from [SalesLT].[Customer] WHERE FirstName='{inputs_dict['FirstName']}' and MiddleName='{inputs_dict['MiddleName']}' and LastName='{inputs_dict['LastName']}'"""
    connectionString = conn['connectionString']
    sqlConn = pyodbc.connect(connectionString) 
    cursor = sqlConn.cursor()
    queryResult = pd.DataFrame()
    try:
      cursor.execute(sqlQuery)
      records = cursor.fetchall()
      queryResult = pd.DataFrame.from_records(records, columns=[col[0] for col in cursor.description])
    except Exception as e:
      print(f"connection could not be established: {e}")
    finally:
      cursor.close()
    
    customer_detail_json = json.loads(queryResult.to_json(orient='records'))
    return customer_detail_json