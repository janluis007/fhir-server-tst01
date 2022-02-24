-- TODO: Consider removing header
/*************************************************************
    Stored procedures for get next available task
**************************************************************/
--
-- STORED PROCEDURE
--     GetNextTask
--
-- DESCRIPTION
--     Get next available tasks
--
-- PARAMETERS
--     @queueId
--         * The ID of the task record
--     @count -- removed
--         * Batch count for tasks list
--     @taskHeartbeatTimeoutThresholdInSeconds
--         * Timeout threshold in seconds for heart keep alive
GO
--DROP PROCEDURE dbo.GetNextTask
GO
CREATE PROCEDURE dbo.GetNextTask
    @queueId varchar(64),
    @TaskHeartbeatTimeoutThresholdInSeconds int = 600
AS
set nocount on
DECLARE @Lock varchar(200) = 'GetNextTask_Q='+@queueId
       ,@TaskId int = NULL

BEGIN TRY
  BEGIN TRANSACTION  

  EXECUTE sp_getapplock @Lock, 'Exclusive'

  -- try old tasks first
  UPDATE T
    SET StartDate = getUTCdate()
       ,HeartBeatDate = getUTCdate()
       ,Worker = host_name() 
       ,@TaskId = T.TaskId
       ,RestartInfo = isnull(RestartInfo,'')+' Prev: Worker='+Worker+' Start='+convert(varchar,getUTCdate(),121) 
    FROM dbo.TaskInfo T WITH (PAGLOCK)
         JOIN (SELECT TOP 1 
                      TaskId
                 FROM dbo.TaskInfo WITH (INDEX = IX_Status)
                 WHERE QueueId = @QueueId
                   AND Status = 2 -- running
                   AND datediff(second, HeartbeatDate, getUTCdate()) >= @TaskHeartbeatTimeoutThresholdInSeconds
                 ORDER BY 
                      TaskId
              ) S
           ON T.QueueId = @QueueId AND T.TaskId = S.TaskId 
  
  IF @TaskId IS NULL
    -- new ones now
    UPDATE T
      SET Status = 2 -- running
         ,StartDate = getUTCdate()
         ,HeartBeatDate = getUTCdate()
         ,Worker = host_name() 
         ,@TaskId = T.TaskId
      FROM dbo.TaskInfo T WITH (PAGLOCK)
           JOIN (SELECT TOP 1 
                        TaskId
                   FROM dbo.TaskInfo WITH (INDEX = IX_Status)
                   WHERE QueueId = @QueueId
                     AND Status = 1 -- Created
                   ORDER BY 
                        TaskId
                ) S
             ON T.QueueId = @QueueId AND T.TaskId = S.TaskId 

  COMMIT TRANSACTION

  EXECUTE dbo.GetTaskDetails @QueueId = @QueueId, @TaskId = @TaskId
END TRY
BEGIN CATCH
  IF @@trancount > 0 ROLLBACK TRANSACTION
  THROW
END CATCH
GO
--GetNextTask '1'
