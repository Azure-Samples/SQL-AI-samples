IF OBJECT_ID('prd.vw_GetSaleDetails', 'V') IS NOT NULL
    DROP VIEW prd.vw_GetSaleDetails;
GO

CREATE VIEW prd.vw_GetSaleDetails
AS
SELECT Country, Category, Color, SUM(OrderQuantity) AS Quantity
FROM prd.sales
GROUP BY Country, Category, Color;
GO