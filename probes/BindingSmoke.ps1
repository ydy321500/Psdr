$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework
$bin = (Resolve-Path (Join-Path $PSScriptRoot '..\src\Psdr.Desktop\bin\Debug')).Path
[void][Reflection.Assembly]::LoadFrom((Join-Path $bin 'Psdr.UI.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $bin 'Psdr.Models.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $bin 'Psdr.Dals.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $bin 'Psdr.Desktop.exe'))

$application = New-Object System.Windows.Application
$theme = New-Object System.Windows.ResourceDictionary
$theme.Source = New-Object System.Uri('pack://application:,,,/Psdr.UI;component/Themes/Theme.xaml')
$application.Resources.MergedDictionaries.Add($theme)

$database = [Psdr.Dals.Configuration.TenantDatabase]::ForConnectionString(
    'Server=.;Database=master;Integrated Security=True;TrustServerCertificate=True')
$productDal = New-Object Psdr.Dals.ProductMap.ProductMapDAL($database)
$dictionaryDal = New-Object Psdr.Dals.Dictionary.DictionaryDAL($database)
$confirmation = New-Object Psdr.Desktop.Services.ConfirmationService
$pages = @(
    @( (New-Object Psdr.Desktop.Views.Set.ProductMap.ProductMapView),
       (New-Object Psdr.Desktop.ViewModels.Set.ProductMap.ProductMapViewModel($productDal)) ),
    @( (New-Object Psdr.Desktop.Views.BaseData.Dictionary.DictionaryView),
       (New-Object Psdr.Desktop.ViewModels.BaseData.Dictionary.DictionaryViewModel(
           $dictionaryDal, $productDal, $confirmation)) )
)

foreach ($page in $pages) {
    $page[0].DataContext = $page[1]
    $window = New-Object System.Windows.Window
    $window.Content = $page[0]
    $window.Width = 1000
    $window.Height = 700
    $window.Show()
    $window.UpdateLayout()
    $window.Close()
}
Write-Output 'PASS: product attribute and dictionary WPF bindings rendered'
