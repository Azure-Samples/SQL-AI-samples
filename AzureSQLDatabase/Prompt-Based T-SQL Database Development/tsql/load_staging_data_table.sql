CREATE PROCEDURE etl_process.usp_BulkInsertFromCSV
    @tableName NVARCHAR(255),
    @filePath NVARCHAR(255),
    @errorFilePath NVARCHAR(255)
AS
BEGIN
    DECLARE @startTime DATETIME;
    DECLARE @endTime DATETIME;
    DECLARE @errorMsg NVARCHAR(MAX);

    BEGIN TRY
        SET @startTime = GETDATE();

        DECLARE @bulkInsertQuery NVARCHAR(MAX);
        SET @bulkInsertQuery = 'TRUNCATE TABLE ' + @tableName + '; ' +
            'BULK INSERT ' + @tableName + ' ' +
            'FROM ''' + @filePath + ''' ' +
            'WITH (FIRSTROW = 2, FIELDTERMINATOR = '','', ROWTERMINATOR = ''\n'', ERRORFILE = ''' + @errorFilePath + ''');';

        EXEC sp_executesql @bulkInsertQuery;

        SET @endTime = GETDATE();

        EXEC etl_process.usp_get_process_log 'Bulk Insert', 'T-SQL', @tableName, @startTime, @endTime;
    END TRY
    BEGIN CATCH
        SET @endTime = GETDATE();
        SET @errorMsg = ERROR_MESSAGE();

        EXEC etl_process.usp_get_error_log 'Bulk Insert', @tableName, @errorMsg, @startTime, @endTime;
    END CATCH;
END;