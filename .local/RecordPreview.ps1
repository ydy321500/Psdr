$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework
$root = Split-Path $PSScriptRoot -Parent
$bin = Join-Path $root 'src\Psdr.Desktop\bin\Debug'
foreach ($name in @('Psdr.UI.dll', 'Psdr.Models.dll', 'Psdr.Dals.dll', 'Psdr.Desktop.exe')) {
    [void][Reflection.Assembly]::LoadFrom((Join-Path $bin $name))
}
$app = New-Object System.Windows.Application
$theme = [System.Windows.Application]::LoadComponent(
    (New-Object System.Uri('/Psdr.UI;component/Themes/Theme.xaml', [System.UriKind]::Relative)))
$app.Resources.MergedDictionaries.Add($theme)
$database = [Psdr.Dals.Configuration.TenantDatabase]::ForConnectionString(
    'Server=.;Database=master;Integrated Security=True;TrustServerCertificate=True')
$dal = New-Object Psdr.Dals.StoreHouse.StoreHouseDAL($database)
$confirmation = New-Object Psdr.Desktop.Services.ConfirmationService
$vm = New-Object Psdr.Desktop.ViewModels.BaseData.StoreHouse.StoreHouseViewModel($dal, $confirmation)
$items = New-Object 'System.Collections.ObjectModel.ObservableCollection[Psdr.Models.Entities.StoreHouse]'
$items.Add((New-Object Psdr.Models.Entities.StoreHouse -Property @{
    StoreHouseID = 1; Name = 'Finished goods'; Remark = 'Warehouse for finished goods'
}))
$items.Add((New-Object Psdr.Models.Entities.StoreHouse -Property @{
    StoreHouseID = 2; Name = 'Raw materials'; Remark = 'Warehouse for raw materials'
}))
[void]$vm.GetType().GetProperty('Items').GetSetMethod($true).Invoke($vm, (,$items.PSObject.BaseObject))
$view = New-Object Psdr.Desktop.Views.BaseData.StoreHouse.StoreHouseView
$view.DataContext = $vm
$window = New-Object System.Windows.Window
$window.Width = 1360
$window.Height = 700
$window.WindowStyle = 'None'
$window.Background = $app.TryFindResource('CanvasBrush')
$window.Content = $view
$window.Show()
$window.UpdateLayout()
$recordButtons = @()
function FindRecordButtons($parent) {
    if ($parent -is [System.Windows.Controls.Button] -and
        $parent.CommandParameter -is [Psdr.Models.Entities.StoreHouse]) {
        $script:recordButtons += $parent
    }
    $count = [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($parent)
    for ($index = 0; $index -lt $count; $index++) {
        FindRecordButtons ([System.Windows.Media.VisualTreeHelper]::GetChild($parent, $index))
    }
}
FindRecordButtons $window
$window.Dispatcher.Invoke([Action]{}, [System.Windows.Threading.DispatcherPriority]::ApplicationIdle)
foreach ($button in $recordButtons) {
    Write-Output "button=$($button.Content) enabled=$($button.IsEnabled) can=$($button.Command.CanExecute($button.CommandParameter)) busy=$($vm.IsBusy) command=$($button.Command) parameter=$($button.CommandParameter)"
}
if ($recordButtons.Count -ne 4 -or @($recordButtons | Where-Object { !$_.IsEnabled -or !$_.Command }).Count -ne 0) {
    throw "Row actions did not bind: $($recordButtons.Count) buttons"
}
$setBusy = $vm.GetType().GetProperty('IsBusy').GetSetMethod($true)
[void]$setBusy.Invoke($vm, @($true))
$window.Dispatcher.Invoke([Action]{}, [System.Windows.Threading.DispatcherPriority]::ApplicationIdle)
if (@($recordButtons | Where-Object { $_.IsEnabled }).Count -ne 0) {
    throw 'Row actions stayed enabled while loading.'
}
[void]$setBusy.Invoke($vm, @($false))
$window.Dispatcher.Invoke([Action]{}, [System.Windows.Threading.DispatcherPriority]::ApplicationIdle)
if (@($recordButtons | Where-Object { !$_.IsEnabled }).Count -ne 0) {
    throw 'Row actions did not re-enable after loading.'
}
$bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(1360,700,96,96,
    [System.Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($window)
$encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$output = Join-Path $PSScriptRoot 'record-preview.png'
$stream = [System.IO.File]::Create($output)
try { $encoder.Save($stream) } finally { $stream.Dispose() }
$vm.SelectedItem = $items[0]
$recordButtons[2].Command.Execute($recordButtons[2].CommandParameter)
if (!$vm.IsEditing -or $vm.SelectedItem.StoreHouseID -ne 2) {
    throw 'Row edit did not target the clicked record.'
}
$window.Close()
Write-Output "PASS: row actions bound and edit targets clicked record. $output"
