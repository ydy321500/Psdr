$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework
$root = Split-Path $PSScriptRoot -Parent
$bin = Join-Path $root 'src\Psdr.Desktop\bin\Debug'
[void][Reflection.Assembly]::LoadFrom((Join-Path $bin 'Psdr.UI.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $bin 'Psdr.Models.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $bin 'Psdr.Dals.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $bin 'Psdr.Desktop.exe'))
$app = New-Object System.Windows.Application
$theme = [System.Windows.Application]::LoadComponent(
    (New-Object System.Uri('/Psdr.UI;component/Themes/Theme.xaml', [System.UriKind]::Relative)))
$app.Resources.MergedDictionaries.Add($theme)
$database = [Psdr.Dals.Configuration.TenantDatabase]::ForConnectionString(
    'Server=.;Database=master;Integrated Security=True;TrustServerCertificate=True')
$dal = New-Object Psdr.Dals.Dashboard.DashboardDAL($database)
$vm = New-Object Psdr.Desktop.ViewModels.HomeViewModel($dal)
foreach ($sample in @(
    @('CustomerCount', 26), @('SupplierCount', 5), @('ProductCount', 9),
    @('WarehouseCount', 3), @('ProductTypeCount', 3), @('MaxBarValue', 26))) {
    $setter = $vm.GetType().GetProperty($sample[0]).GetSetMethod($true)
    [void]$setter.Invoke($vm, @([int]$sample[1]))
}
$types = New-Object 'System.Collections.ObjectModel.ObservableCollection[string]'
foreach ($name in @('Type A', 'Type B', 'Type C')) { $types.Add($name) }
[void]$vm.GetType().GetProperty('ProductTypes').GetSetMethod($true).Invoke($vm, (,$types.PSObject.BaseObject))
$view = New-Object Psdr.Desktop.Views.HomeView
$view.DataContext = $vm
$window = New-Object System.Windows.Window
$window.Width = 1340
$window.Height = 760
$window.WindowStyle = 'None'
$window.Background = $app.TryFindResource('CanvasBrush')
$window.Content = $view
$window.Show()
$vm.GetType().GetProperty('SupplierCount').GetSetMethod($true).Invoke($vm, @([int]6)) | Out-Null
$vm.GetType().GetProperty('SupplierCount').GetSetMethod($true).Invoke($vm, @([int]5)) | Out-Null
$vm.GetType().GetProperty('MaxBarValue').GetSetMethod($true).Invoke($vm, @([int]27)) | Out-Null
$vm.GetType().GetProperty('MaxBarValue').GetSetMethod($true).Invoke($vm, @([int]26)) | Out-Null
$window.UpdateLayout()
$bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(1340,760,96,96,
    [System.Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($window)
$encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$output = Join-Path $PSScriptRoot 'dashboard-preview.png'
$stream = [System.IO.File]::Create($output)
try { $encoder.Save($stream) } finally { $stream.Dispose(); $window.Close() }
Write-Output $output
