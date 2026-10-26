IF OBJECT_ID(N'dbo.StoreHouse', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StoreHouse
    (
        StoreHouseID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name nvarchar(100) NOT NULL,
        Remark nvarchar(500) NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.StoreHouse') AND name = N'UX_StoreHouse_Name'
)
BEGIN
    CREATE UNIQUE INDEX UX_StoreHouse_Name ON dbo.StoreHouse(Name);
END;
