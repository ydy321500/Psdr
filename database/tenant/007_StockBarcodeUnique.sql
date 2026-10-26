-- 非空库存条码在整个代理库中唯一，防止并发入库或绑定造成重复在库条码。
-- 原始旧表将该列定义为 nvarchar(max)，不能作为索引键；应用已将条码限制为 200 字符以内。
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

IF COL_LENGTH(N'dbo.Stock', N'BarCode') = -1
    ALTER TABLE dbo.Stock ALTER COLUMN BarCode nvarchar(200) NULL;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Stock')
      AND name = N'UX_Stock_NonEmptyBarCode'
)
BEGIN
    CREATE UNIQUE INDEX UX_Stock_NonEmptyBarCode
        ON dbo.Stock(BarCode)
        WHERE BarCode IS NOT NULL AND BarCode <> N'';
END;
