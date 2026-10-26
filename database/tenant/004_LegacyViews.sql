-- View definitions reconstructed from NewPsdr view entities and their DAL usage.
-- CREATE OR ALTER must be executed as its own batch.
EXEC(N'CREATE OR ALTER VIEW dbo.View_IndentInfo AS
SELECT i.OrderInfoID, i.No, i.PC, i.ProductID, i.ThisPrice, i.BarCode,
       i.Amount, i.State, i.UpdateTime, h.ShopID, h.Time,
       h.Amount AS TotalAmount, h.TotalMoney, h.OtherMoney, h.Remark,
       p.ProductName, p.K1, p.K2, p.K3, p.K4, p.K5, p.K6, p.K7, p.K8,
       p.V1, p.V2, p.V3, p.V4, p.V5, p.V6
FROM dbo.IndentInfo AS i
LEFT JOIN dbo.Indent AS h ON h.No = i.No
LEFT JOIN dbo.Product AS p ON p.ProductID = i.ProductID');

EXEC(N'CREATE OR ALTER VIEW dbo.View_PersonPriceShopInfo AS
SELECT p.PriceID, p.ShopID, p.ProductID, p.ProductName,
       p.PersonPricesShop, p.PersonRedPacketPriceLow AS PersonFyPsdrPriceLow,
       p.PersonRedPacketPriceHigh AS PersonFyPsdrPriceHigh,
       g.K1, g.K2, g.K3, g.K4, g.K5, g.K6, g.K7, g.K8,
       g.V1, g.V2, g.V3, g.V4, g.V5, g.V6
FROM dbo.PersonPriceShop AS p
LEFT JOIN dbo.Product AS g ON g.ProductID = p.ProductID');

EXEC(N'CREATE OR ALTER VIEW dbo.View_PersonPriceSupplyInfo AS
SELECT p.PriceID, p.SupplyID, p.ProductID, p.ProductName,
       p.PersonPricesSupply,
       g.K1, g.K2, g.K3, g.K4, g.K5, g.K6, g.K7, g.K8,
       g.V1, g.V2, g.V3, g.V4, g.V5, g.V6
FROM dbo.PersonPriceSupply AS p
LEFT JOIN dbo.Product AS g ON g.ProductID = p.ProductID');

EXEC(N'CREATE OR ALTER VIEW dbo.View_StockInfo AS
SELECT s.StockID, s.ProductID, s.BarCode, s.Amount,
       p.Position, p.ProductName,
       p.K1, p.K2, p.K3, p.K4, p.K5, p.K6, p.K7, p.K8,
       p.V1, p.V2, p.V3, p.V4, p.V5, p.V6,
       p.PurchasePrice, p.Price,
       p.RedPacketPriceLow AS FyPsdrPriceLow,
       p.RedPacketPriceHigh AS FyPsdrPriceHigh
FROM dbo.Stock AS s
LEFT JOIN dbo.Product AS p ON p.ProductID = s.ProductID');

EXEC(N'CREATE OR ALTER VIEW dbo.View_StorehouseRecordInfo AS
SELECT i.StorehouseRecordInfoID, i.No, i.ProductID, i.ThisPrice, i.Amount,
       p.ProductName,
       p.K1, p.K2, p.K3, p.K4, p.K5, p.K6, p.K7, p.K8,
       p.V1, p.V2, p.V3, p.V4, p.V5, p.V6
FROM dbo.StorehouseRecordInfo AS i
LEFT JOIN dbo.Product AS p ON p.ProductID = i.ProductID');
