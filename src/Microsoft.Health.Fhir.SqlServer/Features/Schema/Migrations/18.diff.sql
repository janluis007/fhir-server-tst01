/*************************************************************
    Purge partition feature for resource change data
**************************************************************/
--
-- STORED PROCEDURE
--     ConfigurePartitionOnResourceChanges
--
-- DESCRIPTION
--     Creates the initial partitions for the resource change data table.
--
CREATE OR ALTER PROCEDURE dbo.ConfigurePartitionOnResourceChanges
    @numberOfPartitions int = 48
AS
  BEGIN

	--using XACT_ABORT to force a rollback on any error.
	SET XACT_ABORT ON;
	
	BEGIN TRANSACTION
	
		DECLARE @rightPartitionBoundary datetime2(7); 
		DECLARE @currentDate datetime2(7) = sysutcdatetime();	
		DECLARE @startDate datetime2(7);
		DECLARE @numberOfPartitionsCreated int = 0;
		DECLARE @numberOfPartitionsToAdd int = 0;
		
		DECLARE @numberOfPartitionsEarlierThanCurrentDate int = 0;
		DECLARE @numberOfPartitionsLaterThanCurrentDate int = 0;

		SET @numberOfPartitionsEarlierThanCurrentDate = (SELECT count(1)
						FROM   sys.partition_range_values AS partition_range_values
							JOIN sys.partition_functions AS partition_functions
								ON partition_functions.function_id = partition_range_values.function_id
						WHERE  partition_functions.name = N'PartitionFunction_ResourceChangeData_Timestamp'
						and  CONVERT(datetime2(7), partition_range_values.value, 126) < @currentDate);

		SET @numberOfPartitionsLaterThanCurrentDate =  (SELECT count(1)
						FROM   sys.partition_range_values AS partition_range_values
							JOIN sys.partition_functions AS partition_functions
								ON partition_functions.function_id = partition_range_values.function_id
						WHERE  partition_functions.name = N'PartitionFunction_ResourceChangeData_Timestamp'
						and  CONVERT(datetime2(7), partition_range_values.value, 126) > @currentDate);
						
		SET @rightPartitionBoundary = CAST((SELECT TOP (1) value
							FROM   sys.partition_range_values AS partition_range_values
								JOIN sys.partition_functions AS partition_functions
									ON partition_functions.function_id = partition_range_values.function_id
							WHERE  partition_functions.name = N'PartitionFunction_ResourceChangeData_Timestamp'
							ORDER  BY boundary_id DESC) AS datetime2(7));

		IF (@rightPartitionBoundary > @currentDate) BEGIN
			SET @startDate = @rightPartitionBoundary;
		END
		ELSE BEGIN		
			SET @startDate = @currentDate;
		END;

		--excluding the extreme left.
		SET @numberOfPartitionsEarlierThanCurrentDate -= 1;				
		IF (@numberOfPartitionsEarlierThanCurrentDate < 0) BEGIN
			SET @numberOfPartitionsEarlierThanCurrentDate = 0;
		END;

		SET @numberOfPartitionsToAdd = @numberOfPartitions - @numberOfPartitionsEarlierThanCurrentDate - @numberOfPartitionsLaterThanCurrentDate;

		SET @rightPartitionBoundary = DATEADD(hour, DATEDIFF(hour, 0, @startDate), 0)		
		
		WHILE @numberOfPartitionsCreated < @numberOfPartitionsToAdd  
		BEGIN  

			SET @rightPartitionBoundary = DATEADD(hour, 1, @rightPartitionBoundary)

			ALTER PARTITION SCHEME PartitionScheme_ResourceChangeData_Timestamp NEXT USED [PRIMARY];
			ALTER PARTITION FUNCTION PartitionFunction_ResourceChangeData_Timestamp() SPLIT RANGE(@rightPartitionBoundary); 

			SET @numberOfPartitionsCreated += 1; 				
		END;

	COMMIT TRANSACTION
END;
GO 


--
-- STORED PROCEDURE
--     CheckPartitioningForResourceChanges
--
-- DESCRIPTION
--     Checking if the initial partitions are created for the resource change data table.
--
CREATE OR ALTER PROCEDURE dbo.CheckPartitioningForResourceChanges
	@numberOfPartitions int = 48
AS
BEGIN	
	DECLARE @returnValue AS INT;
	DECLARE @numberOfRows AS INT;
	SET @numberOfRows = (SELECT count(1) FROM sys.tables AS t  
		JOIN sys.indexes AS i ON t.object_id = i.object_id  
		JOIN sys.partitions AS p ON i.object_id = p.object_id AND i.index_id = p.index_id   
		JOIN  sys.partition_schemes AS s ON i.data_space_id = s.data_space_id  
		JOIN sys.partition_functions AS f ON s.function_id = f.function_id  
		LEFT JOIN sys.partition_range_values AS r ON f.function_id = r.function_id and r.boundary_id = p.partition_number  
	WHERE t.name = 'ResourceChangeData' AND i.type <= 1 AND
		s.name = 'PartitionScheme_ResourceChangeData_Timestamp' AND
		f.name = 'PartitionFunction_ResourceChangeData_Timestamp');

	IF (@numberOfRows > @numberOfPartitions) 
	BEGIN
		SET @returnValue = 0
	END 
	ELSE BEGIN
		SET @returnValue = -1
	END
		
	RETURN @returnValue
END;
GO 

-- STORED PROCEDURE
--     RemovePartitionFromResourceChanges
--
-- DESCRIPTION
--     Creates a staging table dynamically, and switch out the extreme left partition.
--     After that, it will merge the extreme left partition with the imediate left partition.
--     Finally, drop the staging table to purge the oldest data from resource change data table.
--
CREATE OR ALTER PROCEDURE dbo.RemovePartitionFromResourceChanges
AS
  BEGIN
	
	--using XACT_ABORT to force a rollback on any error.
	SET XACT_ABORT ON;
	
	BEGIN TRANSACTION

		DECLARE @stagingTableName nvarchar(128);
		DECLARE @sqlToCreateStagingTable nvarchar(512);
		DECLARE @sqlToAddPrimaryKey nvarchar(512);
		DECLARE @sqlToAddPartitionCheck nvarchar(512);
		DECLARE @sqlToCheckPartitionConstraint nvarchar(512);
		DECLARE @sqlToSwitchOutLeftPartition nvarchar(512);
		DECLARE @sqlToMergeLeftPartition nvarchar(512);
		DECLARE @sqlToDropStagingTable nvarchar(512);
		DECLARE @leftPartitionBoundary datetime2(7);
		DECLARE @boundaryToSwitchOut datetime2(7);
		DECLARE @guid uniqueidentifier = NEWID(); 

		SET @stagingTableName = CONCAT('stagingRCD_', REPLACE(@guid, '-', '_'));

		SET @leftPartitionBoundary = CAST((SELECT TOP (1) value
							FROM   sys.partition_range_values AS partition_range_values
								JOIN sys.partition_functions AS partition_functions
									ON partition_functions.function_id = partition_range_values.function_id
							WHERE  partition_functions.name = N'PartitionFunction_ResourceChangeData_Timestamp'
							ORDER  BY boundary_id ASC) AS datetime2(7));
							
		SET @boundaryToSwitchOut = CAST((SELECT TOP (1) value
							FROM   sys.partition_range_values AS partition_range_values
								JOIN sys.partition_functions AS partition_functions
									ON partition_functions.function_id = partition_range_values.function_id
							WHERE  partition_functions.name = N'PartitionFunction_ResourceChangeData_Timestamp' 
							and CONVERT(datetime2(7), partition_range_values.value, 126) > @leftPartitionBoundary
							ORDER  BY boundary_id ASC) AS datetime2(7));

		SET @sqlToCreateStagingTable = CONCAT('CREATE TABLE dbo.', @stagingTableName, ' (
			Id bigint NOT NULL,
			Timestamp datetime2(7) NOT NULL CONSTRAINT DF_', @stagingTableName, '_Timestamp DEFAULT sysutcdatetime(),
			ResourceId varchar(64) NOT NULL,
			ResourceTypeId smallint NOT NULL,
			ResourceVersion int NOT NULL,
			ResourceChangeTypeId tinyint NOT NULL
		) ON [PRIMARY]');

		SET @sqlToAddPrimaryKey = CONCAT('ALTER TABLE dbo.', @stagingTableName, ' ADD  CONSTRAINT PK_', @stagingTableName, '_TimestampId PRIMARY KEY CLUSTERED(Timestamp ASC, Id ASC) ON [PRIMARY]');

		SET @sqlToAddPartitionCheck = CONCAT('ALTER TABLE dbo.', @stagingTableName, '  WITH CHECK ADD CONSTRAINT chk_', @stagingTableName, '_partition_2 CHECK  (Timestamp<N''', CONVERT(varchar, @boundaryToSwitchOut, 126), ''')');
		SET @sqlToCheckPartitionConstraint = CONCAT('ALTER TABLE dbo.', @stagingTableName, ' CHECK CONSTRAINT chk_', @stagingTableName, '_partition_2');
		
		SET @sqlToSwitchOutLeftPartition = CONCAT('ALTER TABLE dbo.ResourceChangeData SWITCH PARTITION 2 TO dbo.', @stagingTableName);
		SET @sqlToMergeLeftPartition = CONCAT('ALTER PARTITION FUNCTION PartitionFunction_ResourceChangeData_Timestamp() MERGE RANGE(N''', CONVERT(varchar, @leftPartitionBoundary, 126), ''')');
		SET @sqlToDropStagingTable = CONCAT('drop table dbo.', @stagingTableName);

		EXECUTE sp_executesql @sqlToCreateStagingTable
		EXECUTE sp_executesql @sqlToAddPrimaryKey
		EXECUTE sp_executesql @sqlToAddPartitionCheck
		EXECUTE sp_executesql @sqlToCheckPartitionConstraint
		EXECUTE sp_executesql @sqlToSwitchOutLeftPartition
		EXECUTE sp_executesql @sqlToMergeLeftPartition
		EXECUTE sp_executesql @sqlToDropStagingTable

	COMMIT TRANSACTION
END;
GO 

--
-- STORED PROCEDURE
--     AddPartitionOnResourceChanges
--
-- DESCRIPTION
--     Creates a new partition at right for the or the future date which will be
--     the next day of the right most partition boundry.
--
CREATE OR ALTER PROCEDURE dbo.AddPartitionOnResourceChanges
AS
  BEGIN
	
	--using XACT_ABORT to force a rollback on any error.
	SET XACT_ABORT ON;
	
	BEGIN TRANSACTION
		DECLARE @rightPartitionBoundary datetime2(7);
		SET @rightPartitionBoundary = CAST((SELECT TOP (1) value
							FROM   sys.partition_range_values AS partition_range_values
								JOIN sys.partition_functions AS partition_functions
									ON partition_functions.function_id = partition_range_values.function_id
							WHERE  partition_functions.name = N'PartitionFunction_ResourceChangeData_Timestamp'
							ORDER  BY boundary_id DESC) AS datetime2(7));
							
		SET @rightPartitionBoundary = DATEADD(hour, 1, @rightPartitionBoundary)				

		ALTER PARTITION SCHEME PartitionScheme_ResourceChangeData_Timestamp NEXT USED [Primary];
		ALTER PARTITION FUNCTION PartitionFunction_ResourceChangeData_Timestamp() SPLIT RANGE(@rightPartitionBoundary); 

	COMMIT TRANSACTION
END;
GO 

--
-- STORED PROCEDURE
--     PurgeResourceChanges
--
-- DESCRIPTION
--     Purges the oldest data from a resource change data table using partition switch out, drop and merge.
--     Adds a new partition at right in resource change data for the future date which will be
--     the next day of the right most partition boundry. 
--
CREATE OR ALTER PROCEDURE dbo.PurgeResourceChanges
	@numberOfPartitions int = 48
AS
  BEGIN
  
	--using XACT_ABORT to force a rollback on any error.
	SET XACT_ABORT ON;
	
	BEGIN TRANSACTION

		
		DECLARE @currentDate datetime2(7) = sysutcdatetime();
		DECLARE @numberOfPartitionsEarlierThanCurrentDate int = 0;
		DECLARE @numberOfPartitionsLaterThanCurrentDate int = 0;
		
		SET @numberOfPartitionsLaterThanCurrentDate = (SELECT count(1)
						FROM   sys.partition_range_values AS partition_range_values
							JOIN sys.partition_functions AS partition_functions
								ON partition_functions.function_id = partition_range_values.function_id
						WHERE  partition_functions.name = N'PartitionFunction_ResourceChangeData_Timestamp'
						and CONVERT(datetime2(7), partition_range_values.value, 126) > @currentDate);
						
		IF @numberOfPartitionsLaterThanCurrentDate < 3 BEGIN	
			-- It will add a new partition for the future date which will be 
			-- the next day or next hour of the right most partition boundry. 
			EXEC dbo.AddPartitionOnResourceChanges
		END; 

		DECLARE @checkoutCount INT = (SELECT count(1) FROM dbo.EventAgentCheckpoint where CheckpointId = 'EventAgentSinglePartition');						
		
		IF @checkoutCount > 0 BEGIN	
		
			DECLARE @boundaryDateTimeToPurge datetime2(7)= DATEADD(hour, -(@numberOfPartitions-1), @currentDate)
			DECLARE @lastProcessedDateTime datetime2(7)= (SELECT CONVERT(datetime2(7), LastProcessedDateTime, 1) FROM dbo.EventAgentCheckpoint where CheckpointId = 'EventAgentSinglePartition');
			SET @lastProcessedDateTime = DATEADD(hour, -1, @lastProcessedDateTime)
			
			SET @numberOfPartitionsEarlierThanCurrentDate = (SELECT count(1)
							FROM   sys.partition_range_values AS partition_range_values
								JOIN sys.partition_functions AS partition_functions
									ON partition_functions.function_id = partition_range_values.function_id
							WHERE  partition_functions.name = N'PartitionFunction_ResourceChangeData_Timestamp'
							and CONVERT(datetime2(7), partition_range_values.value, 126) < @boundaryDateTimeToPurge 
							and CONVERT(datetime2(7), partition_range_values.value, 126) < @lastProcessedDateTime);	
		
			WHILE @numberOfPartitionsEarlierThanCurrentDate > 0  
			BEGIN
				-- It will create a staging table dynamically, and switch out the extreme left partition. 
				-- After that, it will merge the extreme left partition with the imediate left partition.
				-- Finally, drop the staging table to purge the oldest data from resource change data table.
				EXEC dbo.RemovePartitionFromResourceChanges

				SET @numberOfPartitionsEarlierThanCurrentDate -= 1; 				
			END;
		END;

	COMMIT TRANSACTION
END;
GO 

/*************************************************************
    Create partition function and scheme, and create the clustered index for migrating data.
**************************************************************/
SET XACT_ABORT ON

BEGIN TRANSACTION


	IF NOT EXISTS(SELECT * FROM sys.partition_functions WHERE  name = 'PartitionFunction_ResourceChangeData_Timestamp')
	BEGIN
	
		-- Partition function for the ResourceChangeData table.
		-- It is not a fixed-sized partition. It is a sliding window partition.
		-- Adding a range right partition function on a timestamp column. 
		-- Range right means that the actual boundary value belongs to its right partition, 
		-- it is the first value in the right partition.
		CREATE PARTITION FUNCTION PartitionFunction_ResourceChangeData_Timestamp (datetime2(7))
		AS RANGE RIGHT FOR VALUES('1970-01-01T00:00:00.0000000')
	END

	IF NOT EXISTS(SELECT * FROM sys.partition_schemes WHERE  name = 'PartitionScheme_ResourceChangeData_Timestamp')
	BEGIN
	
		-- Partition scheme which uses a partition function called PartitionFunction_ResourceChangeData_Timestamp, 
		-- and places partitions on the PRIMARY filegroup.
		CREATE PARTITION SCHEME PartitionScheme_ResourceChangeData_Timestamp AS PARTITION PartitionFunction_ResourceChangeData_Timestamp ALL TO([PRIMARY])
	END	
	
	
	IF((SELECT count(1) FROM sys.partition_range_values AS partition_range_values
				JOIN sys.partition_functions AS partition_functions ON partition_functions.function_id = partition_range_values.function_id
		WHERE  partition_functions.name = N'PartitionFunction_ResourceChangeData_Timestamp') = 1) BEGIN
						
		-- Creates default partitions
		DECLARE @numberOfPartitions int = 47
		DECLARE @rightPartitionBoundary datetime2(7);

		-- There will be 51 partitions, default 48 partitions, one for the next hour, and 2 partitions for start and end.  
		WHILE @numberOfPartitions >= -1 BEGIN
		
			SET @rightPartitionBoundary	=  DATEADD(hour, DATEDIFF(hour, 0, sysutcdatetime()) - @numberOfPartitions, 0)
			
			ALTER PARTITION SCHEME PartitionScheme_ResourceChangeData_Timestamp NEXT USED [Primary];
			ALTER PARTITION FUNCTION PartitionFunction_ResourceChangeData_Timestamp() SPLIT RANGE(@rightPartitionBoundary); 
			SET @numberOfPartitions -= 1
		END;
	END;
	
	
	IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_ResourceChangeData')
	BEGIN
		-- Drops indices
		ALTER TABLE dbo.ResourceChangeData DROP CONSTRAINT PK_ResourceChangeData WITH ( ONLINE = OFF )
	END
	
	IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_ResourceChangeData_TimestampId')
	BEGIN
	
		-- Adds primary key clustered indices			
		ALTER TABLE [dbo].[ResourceChangeData] ADD CONSTRAINT [PK_ResourceChangeData_TimestampId] PRIMARY KEY CLUSTERED 
		(
			[Timestamp] ASC,
			[Id] ASC
		) ON PartitionScheme_ResourceChangeData_Timestamp(Timestamp)
	END

COMMIT TRANSACTION
GO


