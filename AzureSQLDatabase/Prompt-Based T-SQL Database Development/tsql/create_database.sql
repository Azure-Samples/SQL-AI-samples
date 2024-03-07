IF EXISTS (SELECT name FROM sys.databases WHERE name = 'azureopenai')
BEGIN
    ALTER DATABASE azureopenai SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE azureopenai;
END

CREATE DATABASE azureopenai;
GO

USE azureopenai;
GO