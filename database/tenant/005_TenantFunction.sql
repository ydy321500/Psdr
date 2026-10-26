IF OBJECT_ID(N'dbo.TenantFunction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantFunction
    (
        FunctionID bigint NOT NULL PRIMARY KEY,
        FunctionName nvarchar(100) NOT NULL,
        StopTime datetime2 NOT NULL,
        FunctionType int NOT NULL,
        State int NOT NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.TenantFunction') AND name = N'UX_TenantFunction_FunctionName'
)
    CREATE UNIQUE INDEX UX_TenantFunction_FunctionName ON dbo.TenantFunction(FunctionName);
