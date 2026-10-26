param(
    [string]$Server = ".",
    [string]$Database = "Psdr_Master"
)

$ErrorActionPreference = "Stop"
if ($Database -notmatch '^[A-Za-z][A-Za-z0-9_]{0,127}$') {
    throw "Invalid administrator database name."
}

$master = [System.Data.SqlClient.SqlConnection]::new("Data Source=$Server;Initial Catalog=master;Integrated Security=True;Encrypt=False")
try {
    $master.Open()
    $check = $master.CreateCommand()
    $check.CommandText = "SELECT DB_ID(@database)"
    [void]$check.Parameters.Add("@database", [System.Data.SqlDbType]::NVarChar, 128)
    $check.Parameters["@database"].Value = $Database
    if ($check.ExecuteScalar() -is [System.DBNull]) {
        $create = $master.CreateCommand()
        $create.CommandText = "CREATE DATABASE [$Database]"
        $create.CommandTimeout = 120
        [void]$create.ExecuteNonQuery()
    }
}
finally { $master.Dispose() }

$connection = [System.Data.SqlClient.SqlConnection]::new("Data Source=$Server;Initial Catalog=$Database;Integrated Security=True;Encrypt=False")
try {
    $connection.Open()
    foreach ($schemaName in @('001_Agent.sql', '002_AdminUser.sql', '003_LegacyTables.sql')) {
        $command = $connection.CreateCommand()
        $command.CommandText = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot "..\database\master\$schemaName"))
        $command.CommandTimeout = 120
        [void]$command.ExecuteNonQuery()
    }

    $migrateAdmin = $connection.CreateCommand()
    $migrateAdmin.CommandText = @'
UPDATE dbo.AdminUser SET UserCode=N'sys'
WHERE UserCode=N'admin'
  AND NOT EXISTS (SELECT 1 FROM dbo.AdminUser WHERE UserCode=N'sys')
'@
    [void]$migrateAdmin.ExecuteNonQuery()

    $checkAdmin = $connection.CreateCommand()
    $checkAdmin.CommandText = "SELECT COUNT(*) FROM dbo.AdminUser WHERE UserCode=N'sys'"
    if ([int]$checkAdmin.ExecuteScalar() -eq 0) {
        $random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
        try {
            $salt = [byte[]]::new(16)
            $random.GetBytes($salt)
        }
        finally { $random.Dispose() }
        $password = '123456'
        $derive = [System.Security.Cryptography.Rfc2898DeriveBytes]::new(
            $password, $salt, 100000, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
        try { $hash = [Convert]::ToBase64String($derive.GetBytes(32)) }
        finally { $derive.Dispose() }
        $encoded = 'PBKDF2-SHA256$100000$' + [Convert]::ToBase64String($salt) + '$' + $hash

        $insert = $connection.CreateCommand()
        $insert.CommandText = 'INSERT dbo.AdminUser (UserCode, DisplayName, PasswordHash, IsEnabled) VALUES (@code, @name, @hash, 1)'
        [void]$insert.Parameters.Add('@code', [System.Data.SqlDbType]::NVarChar, 50)
        [void]$insert.Parameters.Add('@name', [System.Data.SqlDbType]::NVarChar, 100)
        [void]$insert.Parameters.Add('@hash', [System.Data.SqlDbType]::NVarChar, 300)
        $insert.Parameters['@code'].Value = 'sys'
        $insert.Parameters['@name'].Value = '系统管理员'
        $insert.Parameters['@hash'].Value = $encoded
        [void]$insert.ExecuteNonQuery()

        $password = $null
        Write-Output 'Created administrator account sys. Use sys / 123456 on the unified login page.'
    }
    Write-Output "Administrator database $Database is ready on $Server."
}
finally { $connection.Dispose() }
