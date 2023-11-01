from promptflow import tool
import pandas as pd
from promptflow.connections import CustomConnection
import pyodbc
import json
import re


def get_product_category_name(inputs: list):
    product_category_name_lists = set()
    for i in range(len(inputs)):
        pc_name = inputs[i]['ProductCategoryName']
        if pc_name not in product_category_name_lists:
            product_category_name_lists.add(pc_name)
    return re.sub(r',(?=\))', '', str(tuple(product_category_name_lists)))


@tool
def get_product_stats(inputs: list, conn: CustomConnection):
    product_category_name_lists = get_product_category_name(inputs)
    sqlQuery = """SELECT TOP 10 pc.Name AS ProductCategoryName, p.Name, p.Color, p.ListPrice, count(p.Name) as sales_count
FROM SalesLT.Product p
JOIN SalesLT.SalesOrderDetail sod
ON p.ProductID = sod.ProductID
JOIN SalesLT.ProductCategory pc
ON pc.ProductCategoryID = p.ProductCategoryID
WHERE pc.Name IN {product_category_name_lists}
GROUP BY pc.Name, p.Name, p.Color, p.ListPrice
ORDER by pc.Name, sales_count DESC""".replace("{product_category_name_lists}", product_category_name_lists)
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
