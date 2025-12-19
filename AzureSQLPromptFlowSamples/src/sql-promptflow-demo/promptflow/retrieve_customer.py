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
       inputs_dict = json.loads(inputs)
    else:
       inputs_dict = inputs
    first_name = inputs_dict['FirstName']
    last_name = inputs_dict['LastName']
    if inputs_dict['MiddleName'] == "":
      middle_name = "NULL"
    else: 
      middle_name = inputs_dict['MiddleName']
    sqlQuery = f"""select * from [SalesLT].[Customer] WHERE FirstName=? and MiddleName=? and LastName=?"""
    connectionString = conn['connectionString']
    sqlConn = pyodbc.connect(connectionString) 
    cursor = sqlConn.cursor()
    queryResult = pd.DataFrame()
    try:
      cursor.execute(sqlQuery, (first_name, middle_name, last_name))
      records = cursor.fetchall()
      queryResult = pd.DataFrame.from_records(records, columns=[col[0] for col in cursor.description])
    except Exception as e:
      print(f"connection could not be established: {e}")
    finally:
      cursor.close()
    
    customer_detail_json = json.loads(queryResult.to_json(orient='records'))
    return customer_detail_json