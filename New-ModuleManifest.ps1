$moduleName = "PsMarkdownTree"
$modulePath = $env:PsModulePath.Split(';')[-1]
$modulePath = Join-Path $modulePath $moduleName

$modulePath = if (-not (Test-Path $modulePath)) {
    mkdir $modulePath
}
else {
    Get-Item mkdir
}

$items = $modulePath |
    ForEach-Object FullName |
    Join-Path *.dll |
    Get-ChildItem

if (-not $items) {
    Copy-Item `
        -Path "$PsScriptRoot/res/*.dll" `
        -Destination $modulePath
}

New-ModuleManifest `
    -Path (Join-Path $modulePath $moduleName) `
    -RootModule "$moduleName.dll"

