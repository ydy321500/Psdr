$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework
$root = Split-Path $PSScriptRoot -Parent
[void][Reflection.Assembly]::LoadFrom((Join-Path $root 'src\Psdr.UI\bin\Debug\Psdr.UI.dll'))
$app = New-Object System.Windows.Application
$theme = [System.Windows.Application]::LoadComponent(
    (New-Object System.Uri('/Psdr.UI;component/Themes/Theme.xaml', [System.UriKind]::Relative)))
$app.Resources.MergedDictionaries.Add($theme)
$tabs = New-Object System.Windows.Controls.TabControl
$tabs.Style = $app.TryFindResource('WorkspaceTabControlStyle')
foreach ($title in @('工作台', '产品属性', '仓库维护', '客户维护')) {
    $tab = New-Object System.Windows.Controls.TabItem
    $tab.Style = $app.TryFindResource('WorkspaceTabItemStyle')
    $header = New-Object System.Windows.Controls.StackPanel
    $header.Orientation = 'Horizontal'
    $label = New-Object System.Windows.Controls.TextBlock
    $label.Text = $title
    [void]$header.Children.Add($label)
    if ($title -ne '工作台') {
        $close = New-Object System.Windows.Controls.Button
        $close.Content = '×'
        $close.Style = $app.TryFindResource('WorkspaceTabCloseButtonStyle')
        [void]$header.Children.Add($close)
    }
    $tab.Header = $header
    [void]$tabs.Items.Add($tab)
}
$tabs.SelectedIndex = 3
$window = New-Object System.Windows.Window
$window.Width = 900
$window.Height = 160
$window.WindowStyle = 'None'
$window.Content = $tabs
$window.Show()
$window.UpdateLayout()
$bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(900,160,96,96,
    [System.Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($window)
$encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$output = Join-Path $PSScriptRoot 'tab-preview.png'
$stream = [System.IO.File]::Create($output)
try { $encoder.Save($stream) } finally { $stream.Dispose(); $window.Close() }
Write-Output $output
