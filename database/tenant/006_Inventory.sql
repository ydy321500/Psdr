-- 库存余额按仓库维护；业务单据与库存流水共同构成可追溯库存。
IF COL_LENGTH(N'dbo.Stock', N'StoreHouseID') IS NULL
    ALTER TABLE dbo.Stock ADD StoreHouseID int NULL;

IF COL_LENGTH(N'dbo.StorehouseRecord', N'StoreHouseID') IS NULL
    ALTER TABLE dbo.StorehouseRecord ADD StoreHouseID int NULL;

IF COL_LENGTH(N'dbo.StorehouseRecordInfo', N'StoreHouseID') IS NULL
    ALTER TABLE dbo.StorehouseRecordInfo ADD StoreHouseID int NULL;

IF COL_LENGTH(N'dbo.Indent', N'StoreHouseID') IS NULL
    ALTER TABLE dbo.Indent ADD StoreHouseID int NULL;

IF COL_LENGTH(N'dbo.[Return]', N'StoreHouseID') IS NULL
    ALTER TABLE dbo.[Return] ADD StoreHouseID int NULL;

IF OBJECT_ID(N'dbo.StockLedger', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockLedger
    (
        LedgerID bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BusinessNo nvarchar(128) NOT NULL,
        BusinessType nvarchar(30) NOT NULL,
        StoreHouseID int NOT NULL,
        ProductID int NOT NULL,
        BarCode nvarchar(200) NULL,
        ChangeAmount int NOT NULL,
        BalanceAfter int NOT NULL,
        OccurredAt datetime2 NOT NULL,
        Remark nvarchar(500) NULL
    );
    CREATE INDEX IX_StockLedger_Warehouse_Product_Time
        ON dbo.StockLedger(StoreHouseID, ProductID, OccurredAt DESC);
END;
