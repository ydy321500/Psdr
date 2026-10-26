-- Generated from NewPsdr SqlSugar table entities. Lengths/precision are explicit migration choices.
-- The original project contains entity metadata, not the original SQL Server DDL.
IF OBJECT_ID(N'dbo.[BarCode]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[BarCode]
    (
        [BarCodeID] bigint IDENTITY(1,1) NOT NULL,
        [IndexNo] bigint NOT NULL,
        [ClearCode] nvarchar(max) NULL,
        [SecretCode] nvarchar(max) NULL,
        [Time] datetime2 NULL,
        [State] int NULL,
        CONSTRAINT [PK_BarCode] PRIMARY KEY ([BarCodeID])
    );
END;

IF OBJECT_ID(N'dbo.[Dictionary]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Dictionary]
    (
        [DicID] int IDENTITY(1,1) NOT NULL,
        [ProductName] nvarchar(max) NULL,
        [DicName] nvarchar(max) NULL,
        [DicValue] nvarchar(max) NULL,
        [DicModel] nvarchar(max) NULL,
        CONSTRAINT [PK_Dictionary] PRIMARY KEY ([DicID])
    );
END;

IF OBJECT_ID(N'dbo.[FinanceShop]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[FinanceShop]
    (
        [FinanceID] bigint IDENTITY(1,1) NOT NULL,
        [ShopID] int NULL,
        [Money] decimal(18,4) NOT NULL,
        [Time] datetime2 NULL,
        [Type] int NULL,
        [No] nvarchar(max) NULL,
        [Postage] decimal(18,4) NULL,
        [State] int NOT NULL,
        [Remark] nvarchar(max) NULL,
        CONSTRAINT [PK_FinanceShop] PRIMARY KEY ([FinanceID])
    );
END;

IF OBJECT_ID(N'dbo.[FinanceSupply]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[FinanceSupply]
    (
        [FinanceID] bigint IDENTITY(1,1) NOT NULL,
        [SupplyID] int NULL,
        [Money] decimal(18,4) NULL,
        [Time] datetime2 NULL,
        [Type] int NULL,
        [Name] nvarchar(max) NULL,
        [No] nvarchar(max) NULL,
        [Postage] decimal(18,4) NULL,
        [State] int NOT NULL,
        [Remark] nvarchar(max) NULL,
        CONSTRAINT [PK_FinanceSupply] PRIMARY KEY ([FinanceID])
    );
END;

IF OBJECT_ID(N'dbo.[Indent]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Indent]
    (
        [OrderID] bigint IDENTITY(1,1) NOT NULL,
        [No] nvarchar(128) NOT NULL,
        [ShopID] int NULL,
        [Time] datetime2 NULL,
        [ProductName] nvarchar(max) NULL,
        [Amount] int NULL,
        [TotalMoney] decimal(18,4) NULL,
        [OtherMoney] decimal(18,4) NULL,
        [Remark] nvarchar(max) NULL,
        [State] int NULL,
        CONSTRAINT [PK_Indent] PRIMARY KEY ([No])
    );
END;

IF OBJECT_ID(N'dbo.[IndentInfo]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[IndentInfo]
    (
        [OrderInfoID] bigint IDENTITY(1,1) NOT NULL,
        [No] nvarchar(max) NULL,
        [PC] bigint NULL,
        [ProductID] int NULL,
        [Amount] int NULL,
        [ThisPrice] decimal(18,4) NULL,
        [BarCode] nvarchar(max) NULL,
        [State] int NULL,
        [UpdateTime] datetime2 NULL,
        CONSTRAINT [PK_IndentInfo] PRIMARY KEY ([OrderInfoID])
    );
END;

IF OBJECT_ID(N'dbo.[Ini]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Ini]
    (
        [IniID] int IDENTITY(1,1) NOT NULL,
        [Type] int NULL,
        [Title] nvarchar(max) NULL,
        [Value] nvarchar(max) NULL,
        [State] int NULL,
        CONSTRAINT [PK_Ini] PRIMARY KEY ([IniID])
    );
END;

IF OBJECT_ID(N'dbo.[Log]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Log]
    (
        [LogID] bigint IDENTITY(1,1) NOT NULL,
        [LogType] int NOT NULL,
        [UserName] nvarchar(max) NULL,
        [MachineInfo] nvarchar(max) NULL,
        [Event] nvarchar(max) NULL,
        [Info] nvarchar(max) NULL,
        [LogTime] datetime2 NULL,
        [State] int NULL,
        CONSTRAINT [PK_Log] PRIMARY KEY ([LogID])
    );
END;

IF OBJECT_ID(N'dbo.[M_Cart]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[M_Cart]
    (
        [ID] int IDENTITY(1,1) NOT NULL,
        [ProductID] int NOT NULL,
        [Amount] int NOT NULL,
        [UserCode] nvarchar(max) NULL,
        CONSTRAINT [PK_M_Cart] PRIMARY KEY ([ID])
    );
END;

IF OBJECT_ID(N'dbo.[M_Indent]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[M_Indent]
    (
        [IndentID] int IDENTITY(1,1) NOT NULL,
        [IndentNo] nvarchar(max) NULL,
        [ProductID] int NOT NULL,
        [Amount] int NOT NULL,
        [State] int NOT NULL,
        CONSTRAINT [PK_M_Indent] PRIMARY KEY ([IndentID])
    );
END;

IF OBJECT_ID(N'dbo.[M_Product]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[M_Product]
    (
        [ID] int IDENTITY(1,1) NOT NULL,
        [ProductID] int NOT NULL,
        [ProductType] int NOT NULL,
        [ProductName] nvarchar(max) NULL,
        [Price] decimal(18,4) NULL,
        [KeyWord] nvarchar(max) NULL,
        [ImageUrl] nvarchar(max) NULL,
        CONSTRAINT [PK_M_Product] PRIMARY KEY ([ID])
    );
END;

IF OBJECT_ID(N'dbo.[M_User]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[M_User]
    (
        [UserID] int IDENTITY(1,1) NOT NULL,
        [UserCode] nvarchar(max) NULL,
        [UserName] nvarchar(max) NULL,
        [UserPsw] nvarchar(max) NULL,
        [Tel] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [State] int NOT NULL,
        CONSTRAINT [PK_M_User] PRIMARY KEY ([UserID])
    );
END;

IF OBJECT_ID(N'dbo.[PersonPriceShop]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[PersonPriceShop]
    (
        [PriceID] bigint IDENTITY(1,1) NOT NULL,
        [ProductName] nvarchar(max) NULL,
        [ShopID] int NOT NULL,
        [ProductID] bigint NOT NULL,
        [PersonPricesShop] decimal(18,4) NULL,
        [PersonRedPacketPriceLow] decimal(18,4) NULL,
        [PersonRedPacketPriceHigh] decimal(18,4) NULL,
        CONSTRAINT [PK_PersonPriceShop] PRIMARY KEY ([ShopID], [ProductID])
    );
END;

IF OBJECT_ID(N'dbo.[PersonPriceSupply]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[PersonPriceSupply]
    (
        [PriceID] bigint IDENTITY(1,1) NOT NULL,
        [SupplyID] int NOT NULL,
        [ProductID] bigint NOT NULL,
        [ProductName] nvarchar(max) NULL,
        [PersonPricesSupply] decimal(18,4) NULL,
        CONSTRAINT [PK_PersonPriceSupply] PRIMARY KEY ([SupplyID], [ProductID])
    );
END;

IF OBJECT_ID(N'dbo.[Product]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Product]
    (
        [ProductID] int IDENTITY(1,1) NOT NULL,
        [ProductName] nvarchar(max) NULL,
        [K1] nvarchar(max) NULL,
        [K2] nvarchar(max) NULL,
        [K3] nvarchar(max) NULL,
        [K4] nvarchar(max) NULL,
        [K5] nvarchar(max) NULL,
        [K6] nvarchar(max) NULL,
        [K7] nvarchar(max) NULL,
        [K8] nvarchar(max) NULL,
        [V1] nvarchar(max) NULL,
        [V2] nvarchar(max) NULL,
        [V3] nvarchar(max) NULL,
        [V4] nvarchar(max) NULL,
        [V5] nvarchar(max) NULL,
        [V6] nvarchar(max) NULL,
        [PurchasePrice] decimal(18,4) NULL,
        [Price] decimal(18,4) NULL,
        [RedPacketPriceLow] decimal(18,4) NULL,
        [RedPacketPriceHigh] decimal(18,4) NULL,
        [Position] nvarchar(max) NULL,
        [Volume] real NULL,
        CONSTRAINT [PK_Product] PRIMARY KEY ([ProductID])
    );
END;

IF OBJECT_ID(N'dbo.[ProductMap]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[ProductMap]
    (
        [MapID] int IDENTITY(1,1) NOT NULL,
        [ProductName] nvarchar(max) NULL,
        [KeyName] nvarchar(max) NULL,
        [ValueName] nvarchar(max) NULL,
        [FieldType] nvarchar(max) NULL,
        [Remark] nvarchar(max) NULL,
        [State] int NULL,
        CONSTRAINT [PK_ProductMap] PRIMARY KEY ([MapID])
    );
END;

IF OBJECT_ID(N'dbo.[ProductResponding]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[ProductResponding]
    (
        [ProductResID] int IDENTITY(1,1) NOT NULL,
        [ShopID] int NOT NULL,
        [ProductID] nvarchar(max) NULL,
        [Price] decimal(18,4) NOT NULL,
        [FyPsdrPriceLow] decimal(18,4) NOT NULL,
        [FyPsdrPriceHigh] decimal(18,4) NOT NULL,
        [state] nvarchar(max) NULL,
        CONSTRAINT [PK_ProductResponding] PRIMARY KEY ([ProductResID])
    );
END;

IF OBJECT_ID(N'dbo.[RedPacketList]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[RedPacketList]
    (
        [RedPacketID] bigint IDENTITY(1,1) NOT NULL,
        [ProductID] int NOT NULL,
        [ShopID] int NOT NULL,
        [BarCode] nvarchar(max) NULL,
        [UpdateTime] datetime2 NULL,
        CONSTRAINT [PK_RedPacketList] PRIMARY KEY ([RedPacketID])
    );
END;

IF OBJECT_ID(N'dbo.[RedPacketRequestList]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[RedPacketRequestList]
    (
        [RedPackeRequestID] bigint IDENTITY(1,1) NOT NULL,
        [ProductID] int NOT NULL,
        [ShopID] int NOT NULL,
        [AgentNo] nvarchar(max) NULL,
        [OpenID] nvarchar(max) NULL,
        [BarCode] nvarchar(max) NULL,
        [Money] decimal(18,4) NOT NULL,
        [InsertTime] datetime2 NULL,
        [UpdateTime] datetime2 NULL,
        CONSTRAINT [PK_RedPacketRequestList] PRIMARY KEY ([RedPackeRequestID])
    );
END;

IF OBJECT_ID(N'dbo.[ReportStyle]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[ReportStyle]
    (
        [ReportID] int IDENTITY(1,1) NOT NULL,
        [ReportName] nvarchar(max) NULL,
        CONSTRAINT [PK_ReportStyle] PRIMARY KEY ([ReportID])
    );
END;

IF OBJECT_ID(N'dbo.[Return]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Return]
    (
        [ReturnID] bigint IDENTITY(1,1) NOT NULL,
        [No] nvarchar(128) NOT NULL,
        [ShopID] int NULL,
        [Time] datetime2 NULL,
        [ProductName] nvarchar(max) NULL,
        [Amount] int NULL,
        [TotalMoney] decimal(18,4) NULL,
        [OtherMoney] decimal(18,4) NULL,
        [Remark] nvarchar(max) NULL,
        [State] int NULL,
        CONSTRAINT [PK_Return] PRIMARY KEY ([No])
    );
END;

IF OBJECT_ID(N'dbo.[ReturnInfo]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[ReturnInfo]
    (
        [ReturnInfoID] bigint IDENTITY(1,1) NOT NULL,
        [No] nvarchar(max) NULL,
        [PC] bigint NULL,
        [ProductID] int NULL,
        [Amount] int NULL,
        [ThisPrice] decimal(18,4) NULL,
        [BarCode] nvarchar(max) NULL,
        [State] int NULL,
        [UpdateTime] datetime2 NULL,
        CONSTRAINT [PK_ReturnInfo] PRIMARY KEY ([ReturnInfoID])
    );
END;

IF OBJECT_ID(N'dbo.[SalesMan]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[SalesMan]
    (
        [SalesID] int IDENTITY(1,1) NOT NULL,
        [Name] nvarchar(max) NULL,
        [State] int NOT NULL,
        CONSTRAINT [PK_SalesMan] PRIMARY KEY ([SalesID])
    );
END;

IF OBJECT_ID(N'dbo.[SalesPerson]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[SalesPerson]
    (
        [SalesPersonID] int IDENTITY(1,1) NOT NULL,
        [OpenID] nvarchar(max) NULL,
        [NickName] nvarchar(max) NULL,
        [ImgUrl] nvarchar(max) NULL,
        [Remark] nvarchar(max) NULL,
        [TotalRedPacket] float NULL,
        [TotalVolume] float NULL,
        [CurrentVolume] float NULL,
        [ShopID] int NOT NULL,
        [ShopName] nvarchar(max) NULL,
        [Time] datetime2 NULL,
        [State] int NULL,
        CONSTRAINT [PK_SalesPerson] PRIMARY KEY ([SalesPersonID])
    );
END;

IF OBJECT_ID(N'dbo.[Shop]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Shop]
    (
        [ShopID] int IDENTITY(1,1) NOT NULL,
        [ShopName] nvarchar(max) NULL,
        [Province] nvarchar(max) NULL,
        [City] nvarchar(max) NULL,
        [Contacts] nvarchar(max) NULL,
        [Tel] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        [Fax] nvarchar(max) NULL,
        [Salesman] nvarchar(max) NULL,
        [Addr] nvarchar(max) NULL,
        [Remark] nvarchar(max) NULL,
        CONSTRAINT [PK_Shop] PRIMARY KEY ([ShopID])
    );
END;

IF OBJECT_ID(N'dbo.[Stock]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Stock]
    (
        [StockID] bigint IDENTITY(1,1) NOT NULL,
        [ProductID] int NULL,
        [BarCode] nvarchar(max) NULL,
        [Amount] int NULL,
        CONSTRAINT [PK_Stock] PRIMARY KEY ([StockID])
    );
END;

IF OBJECT_ID(N'dbo.[StorehouseRecord]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[StorehouseRecord]
    (
        [StorehouseRecordID] bigint IDENTITY(1,1) NOT NULL,
        [No] nvarchar(128) NOT NULL,
        [SupplyID] int NULL,
        [Time] datetime2 NULL,
        [ProductName] nvarchar(max) NULL,
        [Amount] int NULL,
        [TotalMoney] decimal(18,4) NULL,
        [OtherMoney] decimal(18,4) NULL,
        [Remark] nvarchar(max) NULL,
        [State] int NULL,
        CONSTRAINT [PK_StorehouseRecord] PRIMARY KEY ([No])
    );
END;

IF OBJECT_ID(N'dbo.[StorehouseRecordInfo]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[StorehouseRecordInfo]
    (
        [StorehouseRecordInfoID] bigint IDENTITY(1,1) NOT NULL,
        [No] nvarchar(max) NULL,
        [ProductID] int NULL,
        [ThisPrice] decimal(18,4) NULL,
        [Amount] int NULL,
        CONSTRAINT [PK_StorehouseRecordInfo] PRIMARY KEY ([StorehouseRecordInfoID])
    );
END;

IF OBJECT_ID(N'dbo.[Supply]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Supply]
    (
        [SupplyID] int IDENTITY(1,1) NOT NULL,
        [SupplyName] nvarchar(max) NULL,
        [Province] nvarchar(max) NULL,
        [City] nvarchar(max) NULL,
        [Addr] nvarchar(max) NULL,
        [Tel] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        [Fax] nvarchar(max) NULL,
        [Contacts] nvarchar(max) NULL,
        [Remark] nvarchar(max) NULL,
        CONSTRAINT [PK_Supply] PRIMARY KEY ([SupplyID])
    );
END;

