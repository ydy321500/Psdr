$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName System.Data

$name = '确认窗测试_' + [Guid]::NewGuid().ToString('N')
$connection = New-Object System.Data.SqlClient.SqlConnection(
    'Data Source=.;Initial Catalog=Psdr_Dev;Integrated Security=True;Connect Timeout=5')
$id = 0
try {
    $connection.Open()
    $insert = $connection.CreateCommand()
    $insert.CommandText = 'INSERT dbo.StoreHouse (Name) VALUES (@name); SELECT CAST(SCOPE_IDENTITY() AS int)'
    [void]$insert.Parameters.Add('@name', [System.Data.SqlDbType]::NVarChar, 100)
    $insert.Parameters['@name'].Value = $name
    $id = [int]$insert.ExecuteScalar()

    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, 49928)
    $shell = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $condition)
    if (-not $shell) { throw 'Shell missing' }
    $shortcutCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'HomeStoreHouseShortcut')
    $shortcut = $shell.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $shortcutCondition)
    if ($shortcut) {
        $shortcut.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    }
    else {
        $refreshCondition = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, '刷新')
        $refresh = $shell.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $refreshCondition)
        $refresh.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    }
    Start-Sleep -Milliseconds 500

    $textCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $text = $shell.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $textCondition)
    if (-not $text) { throw 'Temporary warehouse row missing' }
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    $row = $text
    while ($row -and $row.Current.ControlType -ne [System.Windows.Automation.ControlType]::DataItem) {
        $row = $walker.GetParent($row)
    }
    if (-not $row) { throw 'Temporary warehouse row could not be selected' }
    $row.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $deleteCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, '删除')
    $delete = $shell.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $deleteCondition)
    $delete.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Start-Sleep -Milliseconds 200
    foreach ($controlId in 'ConfirmAccept','ConfirmCancel','ConfirmClose') {
        $find = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $controlId)
        $control = $shell.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $find)
        if (-not $control) { throw "Custom confirmation missing $controlId" }
    }
    $cancelCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'ConfirmCancel')
    $cancel = $shell.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cancelCondition)
    $cancel.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Write-Output 'PASS: custom confirmation dialog opens and Cancel works'
}
finally {
    if ($id -gt 0) {
        $cleanup = $connection.CreateCommand()
        $cleanup.CommandText = 'DELETE dbo.StoreHouse WHERE StoreHouseID = @id'
        [void]$cleanup.Parameters.Add('@id', [System.Data.SqlDbType]::Int)
        $cleanup.Parameters['@id'].Value = $id
        [void]$cleanup.ExecuteNonQuery()
    }
    $connection.Dispose()
}
