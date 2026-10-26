IF OBJECT_ID(N'dbo.[User]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[User]
    (
        UserID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserCode nvarchar(50) NOT NULL,
        UserName nvarchar(100) NOT NULL,
        UserPassword nvarchar(300) NOT NULL,
        Role nvarchar(500) NULL,
        State int NULL,
        Online int NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.[User]') AND name = N'UX_User_UserCode'
)
BEGIN
    CREATE UNIQUE INDEX UX_User_UserCode ON dbo.[User](UserCode);
END;
