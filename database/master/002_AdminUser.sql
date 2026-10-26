IF OBJECT_ID(N'dbo.AdminUser', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminUser
    (
        AdminUserID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserCode nvarchar(50) NOT NULL,
        DisplayName nvarchar(100) NOT NULL,
        PasswordHash nvarchar(300) NOT NULL,
        IsEnabled bit NOT NULL CONSTRAINT DF_AdminUser_IsEnabled DEFAULT (1)
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.AdminUser') AND name = N'UX_AdminUser_UserCode'
)
    CREATE UNIQUE INDEX UX_AdminUser_UserCode ON dbo.AdminUser(UserCode);
