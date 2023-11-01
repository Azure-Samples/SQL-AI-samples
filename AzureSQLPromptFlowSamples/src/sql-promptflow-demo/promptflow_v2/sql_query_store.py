from promptflow import tool

# English only product description
query_prod_detail = """
                    WITH pro_desc AS(
                        SELECT  P1.ProductDescriptionID, P1.Description, P2.ProductModelID,
                            ROW_NUMBER() OVER(PARTITION BY P2.ProductModelID ORDER BY P1.ProductDescriptionID) AS row_number
                        FROM SalesLT.ProductDescription AS P1
                        INNER JOIN SalesLT.ProductModelProductDescription AS P2
                        ON P2.ProductDescriptionID = P1.ProductDescriptionID
                    ), prod_desc_eng AS(
                        -- English only product descriptions
                        SELECT * FROM pro_desc
                        WHERE row_number=1
                    ), prod_detail AS(
                        SELECT p.ProductID, p.ProductModelID, p.Name, p.Color, p.Size, p.Weight, p.ListPrice, p_des.Description, p_cate.Name AS Category, p_cate.ProductCategoryID
                        FROM SalesLT.Product AS p
                        INNER JOIN prod_desc_eng as p_des
                        ON p.ProductModelID = p_des.ProductModelID
                        INNER JOIN SalesLT.ProductCategory AS p_cate
                        ON p_cate.ProductCategoryID = p.ProductCategoryID
                    )
                    """

# Customer order history
query_order = query_prod_detail + """
                  SELECT p.Name, p.Category, p.Color, p.Size, p.Weight, p.ListPrice, p.Description
                  FROM prod_detail AS p
                  INNER JOIN
                  SalesLT.SalesOrderDetail AS sod
                  ON sod.ProductID = p.ProductID
                  INNER JOIN SalesLT.SalesOrderHeader AS soh
                  ON sod.SalesOrderID = soh.SalesOrderID
                  WHERE soh.CustomerID IN {list_cust}"""

# product detail by id
query_prod_byID = query_prod_detail + """
                  SELECT p.Name, p.Category, p.Color, p.Size, p.Weight, p.ListPrice, p.Description, p.ProductCategoryID
                  FROM prod_detail AS p
                  WHERE p.ProductID IN {list_product}"""

# product sales stats by category id, returns top 5 most saled products for each category in the list
query_sales_stat = query_prod_detail + """, prod_sales AS(
                        SELECT p.Name, p.Category, p.Color, p.Size, p.Weight, p.ListPrice, p.Description, count(p.Name) as sales_count, p.ProductCategoryID,
                                ROW_NUMBER() OVER(PARTITION BY p.ProductCategoryID ORDER BY count(p.Name) DESC) AS row_number
                        FROM prod_detail p
                        INNER JOIN SalesLT.SalesOrderDetail sod
                        ON p.ProductID = sod.ProductID
                        INNER JOIN SalesLT.ProductCategory pc
                        ON pc.ProductCategoryID = p.ProductCategoryID
                        WHERE pc.ProductCategoryID IN {list_cate}
                        GROUP BY p.Name, p.Category, p.Color, p.Size, p.Weight, p.ListPrice, p.Description, p.ProductCategoryID
                    )
                    SELECT p.Name, p.Category, p.Color, p.Size, p.Weight, p.ListPrice, p.Description, p.sales_count
                    FROM prod_sales AS p
                    WHERE p.row_number <= 5"""

@tool
def sql_query_prep():

  return {
    'query_order': query_order,
    'query_prod_byID': query_prod_byID,
    'query_sales_stat': query_sales_stat
  }