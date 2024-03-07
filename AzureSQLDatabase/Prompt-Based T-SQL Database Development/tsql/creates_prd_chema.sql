IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'prd')
BEGIN
    EXEC('CREATE SCHEMA prd')
END
GO