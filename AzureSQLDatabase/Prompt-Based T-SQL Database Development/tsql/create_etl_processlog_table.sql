IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'etl_process_log' AND schema_id = SCHEMA_ID('etl_process'))
BEGIN
    CREATE TABLE etl_process.etl_process_log (
        id INT IDENTITY(1,1),
        processname VARCHAR(50),
        processtype VARCHAR(30),
        objectname VARCHAR(50),
        starttime DATETIME,
        endtime DATETIME
    );
END;