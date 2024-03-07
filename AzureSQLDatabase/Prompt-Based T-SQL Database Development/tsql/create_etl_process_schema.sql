IF NOT EXISTS (SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'etl_process')
BEGIN
    EXEC('CREATE SCHEMA etl_process')
END;