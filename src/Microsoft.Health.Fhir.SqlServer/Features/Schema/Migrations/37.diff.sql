IF EXISTS (SELECT * FROM sys.indexes i INNER JOIN sys.tables t ON t.object_id = i.object_id WHERE t.name = 'TokenSearchParam' AND  i.name = 'IX_TokenSeachParam_SearchParamId_Code_SystemId') BEGIN
	DROP INDEX IX_TokenSeachParam_SearchParamId_Code_SystemId
	ON dbo.TokenSearchParam
END

IF NOT EXISTS (SELECT c.* FROM sys.tables t INNER JOIN sys.columns c ON t.object_id = c.object_id WHERE t.name = 'TokenSearchParam' AND c.name = 'Code' AND c.max_length = 512) BEGIN
	ALTER TABLE dbo.TokenSearchParam ALTER COLUMN Code NVARCHAR(256)
END

IF NOT EXISTS (SELECT c.* FROM sys.tables t INNER JOIN sys.columns c ON t.object_id = c.object_id WHERE t.name = 'TokenSearchParam' AND c.name = 'CodeOverflow') BEGIN
	ALTER TABLE dbo.TokenSearchParam ADD CodeOverflow NVARCHAR(MAX) NULL
END

IF NOT EXISTS (SELECT * FROM sys.indexes i INNER JOIN sys.tables t ON t.object_id = i.object_id WHERE t.name = 'TokenSearchParam' AND  i.name = 'IX_TokenSeachParam_SearchParamId_Code_SystemId') BEGIN
	CREATE NONCLUSTERED INDEX IX_TokenSeachParam_SearchParamId_Code_SystemId
		ON dbo.TokenSearchParam(ResourceTypeId, SearchParamId, Code, ResourceSurrogateId)
		INCLUDE(SystemId) WHERE IsHistory = 0 WITH (DATA_COMPRESSION = PAGE)
		ON PartitionScheme_ResourceTypeId (ResourceTypeId);
END
GO
