--DROP TABLE TaskInfo
GO
CREATE TABLE dbo.TaskInfo
(
   QueueId       varchar(64)  NOT NULL -- not sure we need this. Can we remove? make tinyint?
  ,TaskId        int          NOT NULL IDENTITY(1,1)
  ,Status        tinyint      NOT NULL CONSTRAINT DF_TaskInfo_Status DEFAULT 1 CONSTRAINT CHK_TaskInfo_Status CHECK (Status IN (1,2,3,4))
  ,TaskTypeId    smallint     NOT NULL
  ,CreateDate    datetime     NOT NULL CONSTRAINT DF_TaskInfo_CreateDate DEFAULT getUTCdate()
  ,StartDate     datetime     NULL
  ,EndDate       datetime     NULL
  ,IsCanceled    bit          NOT NULL CONSTRAINT DF_TaskInfo_IsCanceled DEFAULT 0
  ,RetryCount    smallint     NOT NULL
  ,MaxRetryCount smallint     NOT NULL
  ,HeartbeatDate datetime     NOT NULL CONSTRAINT DF_TaskInfo_HeartbeatDateTime DEFAULT getUTCdate()
  ,InputData     varchar(max) NOT NULL -- maybe some other fields can be added to keep valuable info, for example surrogate id ranges.
  ,TaskContext   varchar(max) NULL
  ,Result        varchar(max) NULL
  ,Worker        varchar(100) NULL
  ,RestartInfo   varchar(max) NULL
    
  CONSTRAINT PKC_TaskInfo_QueueId_TaskId PRIMARY KEY CLUSTERED (QueueId, TaskId)
)
GO
CREATE INDEX IX_Status ON dbo.TaskInfo (Status)
GO
--INSERT INTO TaskInfo (QueueId,TaskTypeId,RetryCount,MaxRetryCount,InputData) SELECT '1',3,0,2,'Payload'
