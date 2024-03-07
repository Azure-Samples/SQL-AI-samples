CREATE PROCEDURE etl_process.usp_InsertSalesData
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @processname VARCHAR(100) = 'Insert Sales Data';
    DECLARE @processtype VARCHAR(100) = 'ETL';
    DECLARE @objectname VARCHAR(100) = 'prd.sales';
    DECLARE @starttime DATETIME = GETDATE();
    DECLARE @endtime DATETIME;
    DECLARE @errormsg VARCHAR(MAX);
    
    BEGIN TRY
        -- Insert data into prd.sales table
        INSERT INTO prd.sales (ProductId, ProductName, ProductType, Color, OrderQuantity, Size, Category, Country, Date, PurchasePrice, SellingPrice)
        SELECT 
            CAST(ProductId AS INT),
            ProductName,
            ProductType,
            Color,
            CAST(OrderQuantity AS INT),
            Size,
            Category,
            Country,
            CAST(Date AS DATE),
            CAST(PurchasePrice AS DECIMAL(18,2)),
            CAST(SellingPrice AS DECIMAL(18,2))
        FROM stg.salestmp;
        
        SET @endtime = GETDATE();
        
        -- Log the process details
        EXEC etl_process.usp_get_process_log @processname, @processtype, @objectname, @starttime, @endtime;
    END TRY
    BEGIN CATCH
        SET @endtime = GETDATE();
        SET @errormsg = ERROR_MESSAGE();
        
        -- Log the error details
        EXEC etl_process.usp_get_error_log @processname, @objectname, @errormsg, @starttime, @endtime;
    END CATCH;
END;
GO