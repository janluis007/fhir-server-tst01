-- TODO: Consider removing header
/*************************************************************
    Stored procedures for get task payload
**************************************************************/
--
-- STORED PROCEDURE
--     GetTaskDetails
--
-- DESCRIPTION
--     Get task payload.
--
-- PARAMETERS
--     @taskId
--         * The ID of the task record
--
GO
CREATE PROCEDURE dbo.GetTaskDetails @QueueId varchar(64), @TaskId int
AS
set nocount on
SELECT TaskId
      ,QueueId
      ,Status
      ,TaskTypeId
      ,IsCanceled
      ,RetryCount
      ,MaxRetryCount
      ,HeartbeatDate
      ,InputData
      ,TaskContext
      ,Result
  FROM dbo.TaskInfo
  WHERE QueueId = @QueueId
    AND TaskId = @TaskId
GO
