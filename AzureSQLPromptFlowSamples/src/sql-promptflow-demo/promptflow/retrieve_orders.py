from promptflow import tool
import pandas as pd
from promptflow.connections import CustomConnection
import pyodbc
import json
import re


def get_customer_id(inputs: list):
    customer_ids_list = set()
    for i in range(len(inputs)):
        customer_id = inputs[i]['CustomerID']
        if customer_id not in customer_ids_list:
            customer_ids_list.add(customer_id)
    return re.sub(r',(?=\))', '', str(tuple(customer_ids_list)))


@tool
def get_customer_past_orders(inputs: list, conn: CustomConnection):
    customers_ids = get_customer_id(inputs)
    sqlQuery = """select SOH.CustomerID, SOD.ProductID, SP.Name, SP.ProductNumber, SP.Color, SP.Size, SP.ListPrice, SP.ProductCategoryID, SP.ProductModelID,  PD.ProductDescriptionID, PD.Description
  from [SalesLT].[SalesOrderDetail] SOD
  INNER JOIN  [SalesLT].[SalesOrderHeader] SOH on SOD.SalesOrderID = SOH.SalesOrderID
  INNER JOIN [SalesLT].[Product] SP ON SP.ProductID = SOD.ProductID
  INNER JOIN [SalesLT].[ProductModelProductDescription] PMPD ON PMPD.ProductModelID = SP.ProductModelID
  INNER JOIN [SalesLT].[ProductDescription] PD ON PD.ProductDescriptionID = PMPD.ProductDescriptionID
  WHERE PMPD.Culture = 'en'
  AND CustomerID IN {customers_ids}""".replace("{customers_ids}", customers_ids)
    connectionString = conn['connectionString']
    conn = pyodbc.connect(connectionString)
    cursor = conn.cursor()
    queryResult = pd.DataFrame()
    try:
        cursor.execute(sqlQuery)
        records = cursor.fetchall()
        queryResult = pd.DataFrame.from_records(
            records, columns=[col[0] for col in cursor.description])
    except Exception as e:
        print(f"connection could not be established: {e}")
    finally:
        cursor.close()

    return json.loads(queryResult.to_json(orient='records'))
