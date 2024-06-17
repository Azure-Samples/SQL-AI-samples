IF OBJECT_ID('prd.usp_GetTotalSalesByCountries', 'P') IS NOT NULL
    DROP PROCEDURE prd.usp_GetTotalSalesByCountries;
GO

CREATE PROCEDURE prd.usp_GetTotalSalesByCountries
    @country VARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        Country,
        Category,
        COUNT(*) AS TotalOrders,
        SUM(PurchasePrice) AS TotalPurchasePrice
    FROM 
        prd.sales
    WHERE 
        (@country IS NULL OR Country = @country)
    GROUP BY 
        Country,
        Category
    ORDER BY 
        Country ASC,
        TotalPurchasePrice DESC;
END;
GO