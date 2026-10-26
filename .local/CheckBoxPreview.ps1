$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework
$root = Split-Path $PSScriptRoot -Parent
[void][Reflection.Assembly]::LoadFrom((Join-Path $root 'src\Psdr.UI\bin\Debug\Psdr.UI.dll'))
$app = New-Object System.Windows.Application
$theme = [System.Windows.Application]::LoadComponent(
    (New-Object System.Uri('/Psdr.UI;component/Themes/Theme.xaml', [System.UriKind]::Relative)))
$app.Resources.MergedDictionaries.Add($theme)
$panel = New-Object System.Windows.Controls.StackPanel
$panel.Margin = '24'
foreach ($state in @('True', 'False', 'Indeterminate')) {
    $box = New-Object System.Windows.Controls.CheckBox
    $box.Content = "$state option"
    $box.Margin = '0,0,0,14'
    $box.IsThreeState = $true
    if ($state -eq 'Indeterminate') { $box.IsChecked = $null }
    else { $box.IsChecked = [bool]::Parse($state) }
    $panel.Children.Add($box) | Out-Null
}
$window = New-Object System.Windows.Window
$window.Width = 270
$window.Height = 180
$window.WindowStyle = 'None'
$window.Background = $app.TryFindResource('CanvasBrush')
$window.Content = $panel
$window.Show()
$window.UpdateLayout()
$first = $panel.Children[0]
$check = $first.Template.FindName('CheckMark', $first)
if ($check.Visibility -ne 'Visible') { throw 'Checked visual state did not render.' }
$first.IsChecked = $false
if ($check.Visibility -ne 'Collapsed') { throw 'Unchecked visual state did not render.' }
$first.IsChecked = $true
$bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(270,180,96,96,
    [System.Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($window)
$encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$output = Join-Path $PSScriptRoot 'checkbox-preview.png'
$stream = [System.IO.File]::Create($output)
try { $encoder.Save($stream) } finally { $stream.Dispose() }
$window.Close()
Write-Output "PASS: checkbox states. $output"
