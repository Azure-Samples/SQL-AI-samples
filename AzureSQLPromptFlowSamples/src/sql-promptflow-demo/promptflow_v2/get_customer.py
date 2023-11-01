from promptflow import tool
from promptflow.connections import CustomConnection

import pyodbc
import pandas as pd
import numpy as np
import sqlalchemy as sa
import json

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
def get_customer(customer: str, conn_db: CustomConnection):
    first_name = customer.split()[0]
    last_name = customer.split()[-1]
    customer_query = f"""select * from [SalesLT].[Customer] 
                         WHERE FirstName='{first_name}' AND LastName='{last_name}'"""

    out_df = execute_sql(sql_query=customer_query, conn_db=conn_db)
    out_json = out_df.to_json(orient="records")
    out_dict = json.loads(out_json)

    return out_dict