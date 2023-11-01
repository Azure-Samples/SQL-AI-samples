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
def get_orders(customer: list, sql_query_prep: dict, conn_db:CustomConnection):

    list_cust_id = list(map(lambda x: x['CustomerID'], customer))
    list_cust_id = str(tuple(list_cust_id)).replace(",)", ")")
    order_query = sql_query_prep['query_order'].replace("{list_cust}", list_cust_id)

    try:
        out_df = execute_sql(sql_query=order_query, conn_db=conn_db)
        out_json = out_df.to_json(orient="records")
        out_dict = json.loads(out_json)
    except:
        out_dict = {}

    return out_dict