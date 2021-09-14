/*************************************************************
    Purge partition feature for resource change data
**************************************************************/
--
-- STORED PROCEDURE
--     ConfigurePartitionOnResourceChanges
--
-- DESCRIPTION
--     Creates a staging table and initial partitions for the resource change data table.
--
CREATE OR ALTER PROCEDURE dbo.ConfigurePartitionOnResourceChanges
AS
  BEGIN

	--using XACT_ABORT to force a rollback on any error.
	SET XACT_ABORT ON;
	
	BEGIN TRANSACTION
			
		/* Creates a staging table, adds the default check, and adds index for switching out a partition. */
		
		-- Creates a staging table 
		IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ResourceChangeDataStaging')
		BEGIN
			SELECT TOP(0) * INTO dbo.ResourceChangeDataStaging FROM dbo.ResourceChangeData;
		END;

		-- Cleans up a staging table if there are existing rows.
		TRUNCATE TABLE dbo.ResourceChangeDataStaging;

		-- Creates check for a partition check.
		IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name = 'chk_ResourceChangeDataStaging_partition') 
		BEGIN	
			ALTER TABLE dbo.ResourceChangeDataStaging WITH CHECK 
				ADD CONSTRAINT chk_ResourceChangeDataStaging_partition CHECK(Timestamp < CONVERT(DATETIME2(7), '9999-12-31 23:59:59.9999999'));
		END;

		-- Adds primary key clustered index.		
		IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_ResourceChangeDataStaging_TimestampId')
		BEGIN
			ALTER TABLE dbo.ResourceChangeDataStaging ADD CONSTRAINT PK_ResourceChangeDataStaging_TimestampId 
				PRIMARY KEY CLUSTERED(Timestamp ASC, Id ASC) ON [PRIMARY];
		END
				
		/* Creates the initial partitions for the resource change data table. */	
		
		DECLARE @rightPartitionBoundary datetime2(7); 
		DECLARE @currentDate datetime2(7) = sysutcdatetime();	
		DECLARE @startDate datetime2(7);
		DECLARE @numberOfPartitionsToAdd int = 0;
		DECLARE @numberOfFuturePartitionsToAdd int = 4;		
		DECLARE @numberOfPartitionsLaterThanCurrentDate int;
		
		-- Finds the number of partitions later than current datetime.
		SET @numberOfPartitionsLaterThanCurrentDate =  (SELECT count(1)
						FROM sys.partition_range_values AS prv
							JOIN sys.partition_functions AS pf 
								ON pf.function_id = prv.function_id
						WHERE pf.name = N'PartitionFunction_ResourceChangeData_Timestamp'
							and CONVERT(datetime2(7), prv.value, 126) > @currentDate);
				
		-- Finds the highest boundary value.		
		SET @rightPartitionBoundary = CAST((SELECT TOP (1) value
							FROM sys.partition_range_values AS prv
								JOIN sys.partition_functions AS pf
									ON pf.function_id = prv.function_id
							WHERE  pf.name = N'PartitionFunction_ResourceChangeData_Timestamp'
							ORDER  BY prv.boundary_id DESC) AS datetime2(7));

		SET @numberOfPartitionsToAdd = @numberOfFuturePartitionsToAdd - @numberOfPartitionsLaterThanCurrentDate;
		
		IF (@rightPartitionBoundary > @currentDate) BEGIN
			SET @startDate = @rightPartitionBoundary;
		END
		ELSE BEGIN		
			SET @startDate = @currentDate;
			SET @numberOfPartitionsToAdd += 1;
		END;

		-- Rounds the start datetime to the hour.
		SET @rightPartitionBoundary = DATEADD(hour, DATEDIFF(hour, 0, @startDate), 0)		
		
		WHILE @numberOfPartitionsToAdd > 0 
		BEGIN  
			-- Checks if a partition exists.
			IF NOT EXISTS (SELECT 1 value FROM sys.partition_range_values AS prv
							JOIN sys.partition_functions AS pf
								ON pf.function_id = prv.function_id
						WHERE pf.name = N'PartitionFunction_ResourceChangeData_Timestamp'
							and CONVERT(datetime2(7), prv.value, 126) = @rightPartitionBoundary) 
			BEGIN
				-- Creates new empty partition by creating new boundary value and specifying NEXT USED file group.
				ALTER PARTITION SCHEME PartitionScheme_ResourceChangeData_Timestamp NEXT USED [PRIMARY];
				ALTER PARTITION FUNCTION PartitionFunction_ResourceChangeData_Timestamp() SPLIT RANGE(@rightPartitionBoundary); 
			END;
				
			-- Adds one hour for the next partition.
			SET @rightPartitionBoundary = DATEADD(hour, 1, @rightPartitionBoundary);
			SET @numberOfPartitionsToAdd -= 1; 				
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
AS
BEGIN	
	DECLARE @returnValue AS INT;
	DECLARE @numberOfPartitionsLaterThanCurrentDate AS INT;
	DECLARE @currentDate datetime2(7) = sysutcdatetime();	
	
	SET @numberOfPartitionsLaterThanCurrentDate =  (SELECT count(1)
					FROM sys.partition_range_values AS prv
						JOIN sys.partition_functions AS pf
							ON pf.function_id = prv.function_id
					WHERE pf.name = N'PartitionFunction_ResourceChangeData_Timestamp'
						and CONVERT(datetime2(7), prv.value, 126) > @currentDate);

	IF ((@numberOfPartitionsLaterThanCurrentDate > 0) 
		AND EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ResourceChangeDataStaging')) 
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
--     Switches out and merges the extreme left partition with the imediate left partition.
--     After that, truncates the staging table to purge the old resource change data.
--
CREATE OR ALTER PROCEDURE dbo.RemovePartitionFromResourceChanges
AS
  BEGIN
	
	--using XACT_ABORT to force a rollback on any error.
	SET XACT_ABORT ON;
	
	BEGIN TRANSACTION
	
		-- Finds the lowest boundary value.
		DECLARE @leftPartitionBoundary datetime2(7) = CAST((SELECT TOP (1) value
							FROM sys.partition_range_values AS prv
								JOIN sys.partition_functions AS pf
									ON pf.function_id = prv.function_id
							WHERE pf.name = N'PartitionFunction_ResourceChangeData_Timestamp'
							ORDER BY prv.boundary_id ASC) AS datetime2(7));

		-- Cleans up a staging table if there are existing rows.
		TRUNCATE TABLE dbo.ResourceChangeDataStaging;
		
		-- Switches a partition to the staging table.
		ALTER TABLE dbo.ResourceChangeData SWITCH PARTITION 2 TO dbo.ResourceChangeDataStaging;
		
		-- Merges range to move lower boundary one partition ahead.
		ALTER PARTITION FUNCTION PartitionFunction_ResourceChangeData_Timestamp() MERGE RANGE(@leftPartitionBoundary);
		
		-- Cleans up the staging table to purge resource changes.
		TRUNCATE TABLE dbo.ResourceChangeDataStaging;

	COMMIT TRANSACTION
END;
GO 

--
-- STORED PROCEDURE
--     AddPartitionOnResourceChanges
--
-- DESCRIPTION
--     Creates a new partition at the right for the future date which will be
--     the next hour of the rightmost partition boundry.
--
CREATE OR ALTER PROCEDURE dbo.AddPartitionOnResourceChanges
AS
  BEGIN
	
	--using XACT_ABORT to force a rollback on any error.
	SET XACT_ABORT ON;
	
	BEGIN TRANSACTION
			
		-- Finds the highest boundary value
		DECLARE @rightPartitionBoundary datetime2(7)= CAST((SELECT TOP (1) value
							FROM sys.partition_range_values AS prv
								JOIN sys.partition_functions AS pf
									ON pf.function_id = prv.function_id
							WHERE pf.name = N'PartitionFunction_ResourceChangeData_Timestamp'
							ORDER BY prv.boundary_id DESC) AS datetime2(7));
							
		-- Adds one hour for the next partition.
		SET @rightPartitionBoundary = DATEADD(hour, 1, @rightPartitionBoundary);
		
		-- Creates new empty partition by creating new boundary value and specifying NEXT USED file group.
		ALTER PARTITION SCHEME PartitionScheme_ResourceChangeData_Timestamp NEXT USED [Primary];
		ALTER PARTITION FUNCTION PartitionFunction_ResourceChangeData_Timestamp() SPLIT RANGE(@rightPartitionBoundary);			

	COMMIT TRANSACTION
END;
GO 

/*************************************************************
    Create partition function and scheme, and create the clustered index for migrating data.
**************************************************************/
SET XACT_ABORT ON;

BEGIN TRANSACTION


	IF NOT EXISTS(SELECT * FROM sys.partition_functions WHERE  name = 'PartitionFunction_ResourceChangeData_Timestamp')
	BEGIN
	
		-- Partition function for the ResourceChangeData table.
		-- It is not a fixed-sized partition. It is a sliding window partition.
		-- Adding a range right partition function on a timestamp column. 
		-- Range right means that the actual boundary value belongs to its right partition, 
		-- it is the first value in the right partition.
		CREATE PARTITION FUNCTION PartitionFunction_ResourceChangeData_Timestamp (datetime2(7))
			AS RANGE RIGHT FOR VALUES('1970-01-01T00:00:00.0000000');
	END;

	IF NOT EXISTS(SELECT * FROM sys.partition_schemes WHERE name = 'PartitionScheme_ResourceChangeData_Timestamp')
	BEGIN	
		-- Partition scheme which uses a partition function called PartitionFunction_ResourceChangeData_Timestamp, 
		-- and places partitions on the PRIMARY filegroup.
		CREATE PARTITION SCHEME PartitionScheme_ResourceChangeData_Timestamp 
			AS PARTITION PartitionFunction_ResourceChangeData_Timestamp ALL TO([PRIMARY]);
	END;
	
	
	IF((SELECT count(1) FROM sys.partition_range_values AS prv
				JOIN sys.partition_functions AS pf 
					ON pf.function_id = prv.function_id
			WHERE  pf.name = N'PartitionFunction_ResourceChangeData_Timestamp') = 1) BEGIN
						
		-- Creates default partitions
		DECLARE @numberOfPartitions int = 47;
		DECLARE @rightPartitionBoundary datetime2(7);

		-- There will be 51 partitions, default 48 partitions, one for the next hour, and 2 partitions for start and end.  
		WHILE @numberOfPartitions >= -1 
		BEGIN		
			-- Rounds the start datetime to the hour.
			SET @rightPartitionBoundary	=  DATEADD(hour, DATEDIFF(hour, 0, sysutcdatetime()) - @numberOfPartitions, 0);
			
			-- Creates new empty partition by creating new boundary value and specifying NEXT USED file group.
			ALTER PARTITION SCHEME PartitionScheme_ResourceChangeData_Timestamp NEXT USED [Primary];
			ALTER PARTITION FUNCTION PartitionFunction_ResourceChangeData_Timestamp() SPLIT RANGE(@rightPartitionBoundary); 
			
			SET @numberOfPartitions -= 1;
		END;
	END;
	
	
	IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_ResourceChangeData')
	BEGIN
		-- Drops index.
		ALTER TABLE dbo.ResourceChangeData DROP CONSTRAINT PK_ResourceChangeData WITH (ONLINE = OFF);
	END;
		
	IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_ResourceChangeData_TimestampId')
	BEGIN	
		-- Adds primary key clustered index.
		ALTER TABLE dbo.ResourceChangeData ADD CONSTRAINT PK_ResourceChangeData_TimestampId 
			PRIMARY KEY CLUSTERED(Timestamp ASC, Id ASC) ON PartitionScheme_ResourceChangeData_Timestamp(Timestamp)
	END;

COMMIT TRANSACTION
GO


