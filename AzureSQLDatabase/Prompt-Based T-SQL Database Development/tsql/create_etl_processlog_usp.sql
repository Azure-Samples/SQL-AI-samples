CREATE PROCEDURE etl_process.usp_get_process_log
(
    @processname VARCHAR(50),
    @processtype VARCHAR(30),
    @objectname VARCHAR(50),
    @starttime DATETIME,
    @endtime DATETIME
)
AS
BEGIN
    INSERT INTO etl_process.etl_process_log (processname, processtype, objectname, starttime, endtime)
    VALUES (@processname, @processtype, @objectname, @starttime, @endtime)
END