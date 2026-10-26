$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework
$root = Split-Path $PSScriptRoot -Parent
$bin = Join-Path $root 'src\Psdr.Desktop\bin\Debug'
[void][Reflection.Assembly]::LoadFrom((Join-Path $bin 'Psdr.UI.dll'))
$app = New-Object System.Windows.Application
$theme = [System.Windows.Application]::LoadComponent(
    (New-Object System.Uri('/Psdr.UI;component/Themes/Theme.xaml', [System.UriKind]::Relative)))
$app.Resources.MergedDictionaries.Add($theme)
$panel = New-Object System.Windows.Controls.StackPanel
$panel.Margin = '28'
$panel.Width = 300
$label = New-Object System.Windows.Controls.TextBlock
$label.Text = 'Product type'
$label.Margin = '0,0,0,9'
$panel.Children.Add($label) | Out-Null
$list = New-Object System.Windows.Controls.ComboBox
$list.Items.Add('Finished goods') | Out-Null
$list.Items.Add('Raw materials') | Out-Null
$list.SelectedIndex = 0
$panel.Children.Add($list) | Out-Null
$editableLabel = New-Object System.Windows.Controls.TextBlock
$editableLabel.Text = 'Editable location'
$editableLabel.Margin = '0,24,0,9'
$panel.Children.Add($editableLabel) | Out-Null
$editable = New-Object System.Windows.Controls.ComboBox
$editable.IsEditable = $true
$editable.Items.Add('Shanghai') | Out-Null
$editable.Items.Add('Beijing') | Out-Null
$editable.Text = 'Shanghai'
$panel.Children.Add($editable) | Out-Null
$window = New-Object System.Windows.Window
$window.Width = 430
$window.Height = 240
$window.WindowStyle = 'None'
$window.Background = $app.TryFindResource('CanvasBrush')
$window.Content = $panel
$window.Show()
$window.UpdateLayout()
$window.Dispatcher.Invoke([Action]{}, [System.Windows.Threading.DispatcherPriority]::ApplicationIdle)
if ($list.SelectedItem -ne 'Finished goods') { throw 'List selection failed.' }
if ($editable.Text -ne 'Shanghai') { throw 'Editable selection text failed.' }
$inner = $editable.Template.FindName('PART_EditableTextBox', $editable)
if (!$inner -or $inner.Visibility -ne 'Visible') { throw 'Editable text box is unavailable.' }
$inner.Text = 'Hangzhou'
$window.Dispatcher.Invoke([Action]{}, [System.Windows.Threading.DispatcherPriority]::ApplicationIdle)
if ($editable.Text -ne 'Hangzhou') { throw "Editable text did not sync: $($editable.Text)" }
$list.IsDropDownOpen = $true
$window.Dispatcher.Invoke([Action]{}, [System.Windows.Threading.DispatcherPriority]::ApplicationIdle)
$popup = $list.Template.FindName('PART_Popup', $list)
if (!$popup.IsOpen -or $popup.Child.ActualWidth -lt 290) {
    throw "Dropdown popup failed or has wrong width: $($popup.Child.ActualWidth)"
}
$list.IsDropDownOpen = $false
$bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(430,240,96,96,
    [System.Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($window)
$encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$output = Join-Path $PSScriptRoot 'combo-preview.png'
$stream = [System.IO.File]::Create($output)
try { $encoder.Save($stream) } finally { $stream.Dispose() }
$window.Close()
Write-Output "PASS: selection and editable text. $output"
