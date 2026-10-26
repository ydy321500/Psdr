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
$box = New-Object System.Windows.Controls.TextBox
$box.Text = 'Warehouse name'
$panel.Children.Add($box) | Out-Null
$multi = New-Object System.Windows.Controls.TextBox
$multi.Text = "First line`nSecond line"
$multi.AcceptsReturn = $true
$multi.TextWrapping = 'Wrap'
$multi.Height = 76
$multi.Margin = '0,15,0,0'
$panel.Children.Add($multi) | Out-Null
$password = New-Object System.Windows.Controls.PasswordBox
$password.Style = $app.TryFindResource('LoginPasswordBoxStyle')
$password.Password = '123456'
$password.Margin = '0,15,0,0'
$panel.Children.Add($password) | Out-Null
$window = New-Object System.Windows.Window
$window.Width = 350
$window.Height = 240
$window.WindowStyle = 'None'
$window.Background = $app.TryFindResource('CanvasBrush')
$window.Content = $panel
$window.Show()
$window.UpdateLayout()
$box.Text = 'Updated name'
if ($box.Text -ne 'Updated name') { throw 'Text input failed.' }
$multi.AppendText("`nThird line")
if (!$multi.Text.Contains('Third line')) { throw 'Multiline input failed.' }
if ($password.Password -ne '123456') { throw 'Password input failed.' }
$window.UpdateLayout()
$window.Dispatcher.Invoke([Action]{}, [System.Windows.Threading.DispatcherPriority]::ApplicationIdle)
$bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(350,240,96,96,
    [System.Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($window)
$encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$output = Join-Path $PSScriptRoot 'input-preview.png'
$stream = [System.IO.File]::Create($output)
try { $encoder.Save($stream) } finally { $stream.Dispose() }
$window.Close()
Write-Output "PASS: single, multiline and password input. $output"
