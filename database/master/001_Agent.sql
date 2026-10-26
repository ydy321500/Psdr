IF OBJECT_ID(N'dbo.Agent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Agent
    (
        AgentID uniqueidentifier NOT NULL PRIMARY KEY,
        AgentName nvarchar(100) NOT NULL,
        AgentAddress nvarchar(300) NULL,
        AgentProvince nvarchar(100) NULL,
        AgentCity nvarchar(100) NULL,
        AgentArea nvarchar(100) NULL,
        Contact nvarchar(100) NULL,
        Tel nvarchar(50) NULL,
        BankNo nvarchar(100) NULL,
        BankName nvarchar(100) NULL,
        CreatTime datetime2 NOT NULL,
        PayType int NOT NULL CONSTRAINT DF_Agent_PayType DEFAULT (0),
        WeiXin nvarchar(200) NULL,
        Mchid nvarchar(200) NULL,
        Appid nvarchar(200) NULL,
        Appsecret nvarchar(500) NULL,
        FyPsdrLeft nvarchar(50) NULL,
        State int NOT NULL CONSTRAINT DF_Agent_State DEFAULT (1),
        NeedCheck int NOT NULL CONSTRAINT DF_Agent_NeedCheck DEFAULT (0),
        MaxScanCount int NOT NULL CONSTRAINT DF_Agent_MaxScanCount DEFAULT (0),
        ProvisioningStatus nvarchar(20) NOT NULL
            CONSTRAINT DF_Agent_ProvisioningStatus DEFAULT (N'Pending'),
        ProvisioningError nvarchar(300) NULL
    );
END;

IF COL_LENGTH(N'dbo.Agent', N'ProvisioningStatus') IS NULL
    ALTER TABLE dbo.Agent ADD ProvisioningStatus nvarchar(20) NOT NULL
        CONSTRAINT DF_Agent_ProvisioningStatus_Legacy DEFAULT (N'Unverified');

IF COL_LENGTH(N'dbo.Agent', N'ProvisioningError') IS NULL
    ALTER TABLE dbo.Agent ADD ProvisioningError nvarchar(300) NULL;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Agent') AND name = N'UX_Agent_AgentName'
)
    CREATE UNIQUE INDEX UX_Agent_AgentName ON dbo.Agent(AgentName);

IF OBJECT_ID(N'dbo.AgentFunction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgentFunction
    (
        FunctionID bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AgentID uniqueidentifier NOT NULL,
        FunctionName nvarchar(100) NOT NULL,
        StopTime datetime2 NOT NULL,
        FunctionType int NOT NULL,
        State int NOT NULL,
        CONSTRAINT FK_AgentFunction_Agent FOREIGN KEY (AgentID) REFERENCES dbo.Agent(AgentID)
    );
END;
