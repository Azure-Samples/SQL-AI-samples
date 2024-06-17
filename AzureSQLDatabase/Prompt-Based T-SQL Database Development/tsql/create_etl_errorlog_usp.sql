CREATE PROCEDURE etl_process.usp_get_error_log
(
    @processname VARCHAR(50),
    @objectname VARCHAR(50),
    @errormsg VARCHAR(MAX),
    @starttime DATETIME,
    @endtime DATETIME
)
AS
BEGIN
    INSERT INTO etl_process.error_log (processname, objectname, errormsg, starttime, endtime)
    VALUES (@processname, @objectname, @errormsg, @starttime, @endtime)
END