-- Legacy master tables not already provided by 001_Agent.sql.
IF OBJECT_ID(N'dbo.[NewMessage]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[NewMessage]
    (
        [ID] bigint NOT NULL,
        [ToObject] int NOT NULL,
        [Title] nvarchar(max) NULL,
        [Context] nvarchar(max) NULL,
        [State] int NOT NULL,
        [Time] datetime2 NOT NULL,
        [ReadUser] nvarchar(max) NULL,
        CONSTRAINT [PK_NewMessage] PRIMARY KEY ([ID])
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

IF OBJECT_ID(N'dbo.[RunLog]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[RunLog]
    (
        [LogID] bigint IDENTITY(1,1) NOT NULL,
        [LogTitle] nvarchar(max) NULL,
        [LogContent] nvarchar(max) NULL,
        [LogTime] datetime2 NULL,
        CONSTRAINT [PK_RunLog] PRIMARY KEY ([LogID])
    );
END;

IF OBJECT_ID(N'dbo.[ServerLog]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[ServerLog]
    (
        [LogID] bigint IDENTITY(1,1) NOT NULL,
        [UserName] nvarchar(max) NULL,
        [MachineInfo] nvarchar(max) NULL,
        [Event] nvarchar(max) NULL,
        [Info] nvarchar(max) NULL,
        [LogTime] datetime2 NULL,
        [State] int NULL,
        CONSTRAINT [PK_ServerLog] PRIMARY KEY ([LogID])
    );
END;

