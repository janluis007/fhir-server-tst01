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
-- PARAMETERS
--     @numberOfFuturePartitionsToAdd
--         * The number of partitions to add for feature datetimes.
--
CREATE OR ALTER PROCEDURE dbo.ConfigurePartitionOnResourceChanges
    @numberOfFuturePartitionsToAdd int
AS
  BEGIN

	--using XACT_ABORT to force a rollback on any error.
	SET XACT_ABORT ON;
	
	BEGIN TRANSACTION
				
		/* Creates the partitions for future datetimes on the resource change data table. */	
		
		-- Rounds the current datetime to the hour.
		DECLARE @partitionBoundary datetime2(7) = DATEADD(hour, DATEDIFF(hour, 0, sysutcdatetime()), 0);
		
		-- Finds the highest boundary value.		
		DECLARE @startingRightPartitionBoundary datetime2(7) = CAST((SELECT TOP (1) value
							FROM sys.partition_range_values AS prv
								JOIN sys.partition_functions AS pf
									ON pf.function_id = prv.function_id
							WHERE  pf.name = N'PartitionFunction_ResourceChangeData_Timestamp'
							ORDER  BY prv.boundary_id DESC) AS datetime2(7));
							
		-- Adds one due to starting from the current hour.
		DECLARE @numberOfPartitionsToAdd int = @numberOfFuturePartitionsToAdd + 1;	
		
		WHILE @numberOfPartitionsToAdd > 0 
		BEGIN  
			-- Checks if a partition exists.
			IF (@startingRightPartitionBoundary < @partitionBoundary) 
			BEGIN
				-- Creates new empty partition by creating new boundary value and specifying NEXT USED file group.
				ALTER PARTITION SCHEME PartitionScheme_ResourceChangeData_Timestamp NEXT USED [PRIMARY];
				ALTER PARTITION FUNCTION PartitionFunction_ResourceChangeData_Timestamp() SPLIT RANGE(@partitionBoundary); 
			END;
				
			-- Adds one hour for the next partition.
			SET @partitionBoundary = DATEADD(hour, 1, @partitionBoundary);
			SET @numberOfPartitionsToAdd -= 1; 				
		END;

	COMMIT TRANSACTION
END;
GO

-- STORED PROCEDURE
--     RemovePartitionFromResourceChanges
--
-- DESCRIPTION
--     Switches out and merges the extreme left partition with the imediate left partition.
--     After that, truncates the staging table to purge the old resource change data.
--
--     @partitionBoundary
--         * The output parameter to stores the removed partition boundary.
--
CREATE OR ALTER PROCEDURE dbo.RemovePartitionFromResourceChanges
    @partitionBoundary datetime2(7) OUTPUT
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
		
		SET @partitionBoundary = @leftPartitionBoundary

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
-- PARAMETERS
--     @partitionBoundary
--         * The output parameter to stores the added partition boundary.
--
CREATE OR ALTER PROCEDURE dbo.AddPartitionOnResourceChanges
    @partitionBoundary datetime2(7) OUTPUT
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

		-- Rounds the current datetime to the hour.
        DECLARE @timestamp datetime2(7) =  DATEADD(hour, DATEDIFF(hour, 0, sysutcdatetime()), 0);
        
        -- Ensures the next boundary value is greater than the current datetime.
        IF (@rightPartitionBoundary < @timestamp) BEGIN
	        SET @rightPartitionBoundary = @timestamp;
        END;
							
		-- Adds one hour for the next partition.
		SET @rightPartitionBoundary = DATEADD(hour, 1, @rightPartitionBoundary);
		
		-- Creates new empty partition by creating new boundary value and specifying NEXT USED file group.
		ALTER PARTITION SCHEME PartitionScheme_ResourceChangeData_Timestamp NEXT USED [Primary];
		ALTER PARTITION FUNCTION PartitionFunction_ResourceChangeData_Timestamp() SPLIT RANGE(@rightPartitionBoundary);		
		
		SET @partitionBoundary = @rightPartitionBoundary

	COMMIT TRANSACTION
END;
GO 

--
-- STORED PROCEDURE
--     FetchResourceChanges
--
-- DESCRIPTION
--     Returns the number of resource change records from startId. The start id is inclusive.
--
-- PARAMETERS
--     @startId
--         * The start id of resource change records to fetch.
--     @@lastProcessedDateTime
--         * The last checkpoint datetime.
--     @pageSize
--         * The page size for fetching resource change records.
--
-- RETURN VALUE
--     Resource change data rows.
--
CREATE OR ALTER PROCEDURE dbo.FetchResourceChanges_2
    @startId bigint,
    @lastProcessedDateTime datetime2(7),
    @pageSize smallint
AS
BEGIN

    SET NOCOUNT ON;
	
    -- Given the fact that Read Committed Snapshot isolation level is enabled on the FHIR database, 
    -- using the Repeatable Read isolation level table hint to avoid skipping resource changes 
    -- due to interleaved transactions on the resource change data table.    
    -- In Repeatable Read, the select query execution will be blocked until other open transactions are completed
    -- for rows that match the search condition of the select statement. 
    -- A write transaction (update/delete) on the rows that match 
    -- the search condition of the select statement will wait until the read transaction is completed. 
    -- But, other transactions can insert new rows.
    SELECT TOP(@pageSize) Id,
      Timestamp,
      ResourceId,
      ResourceTypeId,
      ResourceVersion,
      ResourceChangeTypeId
      FROM dbo.ResourceChangeData WITH (REPEATABLEREAD)
    WHERE Timestamp >= @lastProcessedDateTime and Id >= @startId 
    ORDER BY Timestamp ASC, Id ASC;
END
GO

-- Creates a staging table 
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ResourceChangeDataStaging')
BEGIN
    -- Staging table that will be used for partition switch out. 
    CREATE TABLE dbo.ResourceChangeDataStaging 
    (
	    Id bigint IDENTITY(1,1) NOT NULL,
	    Timestamp datetime2(7) NOT NULL CONSTRAINT DF_ResourceChangeDataStaging_Timestamp DEFAULT sysutcdatetime(),
	    ResourceId varchar(64) NOT NULL,
	    ResourceTypeId smallint NOT NULL,
	    ResourceVersion int NOT NULL,
	    ResourceChangeTypeId tinyint NOT NULL,
	    CONSTRAINT PK_ResourceChangeDataStaging_TimestampId PRIMARY KEY (Timestamp, Id)
    ) ON [PRIMARY]
END;
GO

-- Creates check for a partition check.
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name = 'chk_ResourceChangeDataStaging_partition') 
BEGIN	
    ALTER TABLE dbo.ResourceChangeDataStaging WITH CHECK 
	    ADD CONSTRAINT chk_ResourceChangeDataStaging_partition CHECK(Timestamp < CONVERT(DATETIME2(7), '9999-12-31 23:59:59.9999999'));

    ALTER TABLE dbo.ResourceChangeDataStaging CHECK CONSTRAINT chk_ResourceChangeDataStaging_partition;
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
		DECLARE @numberOfPartitions int = 48;
		DECLARE @rightPartitionBoundary datetime2(7);

		-- There will be 51 partition boundaries and 52 partitions, 48 partitions for history, 
        -- one for the current hour, one for the next hour, and 2 partitions for start and end.  
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


