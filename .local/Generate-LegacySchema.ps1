param([string]$LegacyRoot = 'E:\LQ\NewPsdr\FyPsdr\FyPsdrModels')

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$models = Join-Path $workspace 'src\Psdr.Models'
$modelProject = Join-Path $models 'Psdr.Models.csproj'
$serverFiles = Get-ChildItem (Join-Path $LegacyRoot 'ServerTables') -Filter '*.cs' | Sort-Object Name
$tenantFiles = Get-ChildItem (Join-Path $LegacyRoot 'Tables') -Filter '*.cs' | Sort-Object Name
$allFiles = @($serverFiles) + @($tenantFiles)

$project = [xml](Get-Content $modelProject -Raw)
$ns = [System.Xml.XmlNamespaceManager]::new($project.NameTable)
$ns.AddNamespace('p', 'http://schemas.microsoft.com/developer/msbuild/2003')
$group = $project.SelectSingleNode('//p:ItemGroup[p:Compile]', $ns)

foreach ($file in $allFiles) {
    if ($file.Name -in @('Agent.cs', 'StoreHouse.cs', 'User.cs')) { continue }
    $category = if ($file.Directory.Name -eq 'ServerTables') { 'Master' } else { 'Tenant' }
    $folder = Join-Path $models "Entities\$category"
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    $target = Join-Path $folder $file.Name
    $source = [System.IO.File]::ReadAllText($file.FullName)
    $source = [regex]::Replace($source, 'namespace FyPsdrModels(?:\.Tables|\.ServerTables)?', 'namespace Psdr.Models.Entities')
    [System.IO.File]::WriteAllText($target, $source, [System.Text.UTF8Encoding]::new($false))
    $include = "Entities\$category\$($file.Name)"
    if (-not $project.SelectSingleNode("//p:Compile[@Include='$include']", $ns)) {
        $element = $project.CreateElement('Compile', $project.DocumentElement.NamespaceURI)
        $element.SetAttribute('Include', $include)
        [void]$group.AppendChild($element)
    }
}
$project.Save($modelProject)

function Get-TableSpec($file) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $table = [regex]::Match($content, '\[SugarTable\("([^"]+)"\)\]').Groups[1].Value
    if (-not $table) { throw "Missing SugarTable: $($file.FullName)" }
    $props = [regex]::Matches($content, '(?s)((?:\[[^\]]+\]\s*)*)public\s+(?:virtual\s+)?([\w?<>]+)\s+(\w+)\s*\{\s*get\s*;\s*set\s*;\s*\}')
    $columns = @()
    foreach ($prop in $props) {
        $attr = $prop.Groups[1].Value
        $type = $prop.Groups[2].Value
        $name = $prop.Groups[3].Value
        if ($attr -match 'IsIgnore\s*=\s*true' -or $attr -match '\[Navigate' -or $type -notmatch '^(string|int|long|Int64|short|decimal|double|float|bool|DateTime|Guid)\??$') { continue }
        $primary = $attr -match 'IsPrimaryKey\s*=\s*true'
        $identity = $attr -match 'IsIdentity\s*=\s*true'
        $baseType = $type.TrimEnd('?')
        $sqlType = switch ($baseType) {
            string { if ($primary) { 'nvarchar(128)' } else { 'nvarchar(max)' } }
            int { 'int' }
            long { 'bigint' }
            Int64 { 'bigint' }
            short { 'smallint' }
            decimal { 'decimal(18,4)' }
            double { 'float' }
            float { 'real' }
            bool { 'bit' }
            DateTime { 'datetime2' }
            Guid { 'uniqueidentifier' }
        }
        $nullable = if ($primary -or $identity -or ($type -ne 'string' -and -not $type.EndsWith('?'))) { 'NOT NULL' } else { 'NULL' }
        $line = "        [$name] $sqlType"
        if ($identity) { $line += ' IDENTITY(1,1)' }
        $line += " $nullable"
        $columns += [pscustomobject]@{ Name=$name; Line=$line; Primary=$primary }
    }
    [pscustomobject]@{ Table=$table; Columns=$columns }
}

$tenantSql = [System.Collections.Generic.List[string]]::new()
$tenantSql.Add('-- Generated from NewPsdr SqlSugar table entities. Lengths/precision are explicit migration choices.')
$tenantSql.Add('-- The original project contains entity metadata, not the original SQL Server DDL.')
foreach ($file in $tenantFiles) {
    $spec = Get-TableSpec $file
    if ($spec.Table -like 'View_*' -or $spec.Table -in @('StoreHouse','User')) { continue }
    $columnLines = @($spec.Columns | ForEach-Object { $_.Line })
    $keys = @($spec.Columns | Where-Object Primary | ForEach-Object { "[$($_.Name)]" })
    if ($keys.Count -gt 0) { $columnLines += "        CONSTRAINT [PK_$($spec.Table)] PRIMARY KEY ($($keys -join ', '))" }
    $tenantSql.Add("IF OBJECT_ID(N'dbo.[$($spec.Table)]', N'U') IS NULL")
    $tenantSql.Add('BEGIN')
    $tenantSql.Add("    CREATE TABLE dbo.[$($spec.Table)]")
    $tenantSql.Add('    (')
    $tenantSql.Add(($columnLines -join ",`r`n"))
    $tenantSql.Add('    );')
    $tenantSql.Add('END;')
    $tenantSql.Add('')
}
[System.IO.File]::WriteAllLines((Join-Path $workspace 'database\tenant\003_LegacyTables.sql'), $tenantSql, [System.Text.UTF8Encoding]::new($false))

$masterSql = [System.Collections.Generic.List[string]]::new()
$masterSql.Add('-- Legacy master tables not already provided by 001_Agent.sql.')
foreach ($file in $serverFiles) {
    $spec = Get-TableSpec $file
    if ($file.Name -in @('Agent.cs', 'AgentFunction.cs')) { continue }
    $columnLines = @($spec.Columns | ForEach-Object { $_.Line })
    $keys = @($spec.Columns | Where-Object Primary | ForEach-Object { "[$($_.Name)]" })
    if ($keys.Count -gt 0) { $columnLines += "        CONSTRAINT [PK_$($spec.Table)] PRIMARY KEY ($($keys -join ', '))" }
    $masterSql.Add("IF OBJECT_ID(N'dbo.[$($spec.Table)]', N'U') IS NULL")
    $masterSql.Add('BEGIN')
    $masterSql.Add("    CREATE TABLE dbo.[$($spec.Table)]")
    $masterSql.Add('    (')
    $masterSql.Add(($columnLines -join ",`r`n"))
    $masterSql.Add('    );')
    $masterSql.Add('END;')
    $masterSql.Add('')
}
[System.IO.File]::WriteAllLines((Join-Path $workspace 'database\master\003_LegacyTables.sql'), $masterSql, [System.Text.UTF8Encoding]::new($false))
Write-Output "Imported $($allFiles.Count - 3) entities and generated master/tenant table scripts."
