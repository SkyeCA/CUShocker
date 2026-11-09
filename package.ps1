# You may need to run the follwing command for this script to work:
#   powershell -ExecutionPolicy Bypass -File package.ps1

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$desktopVersion = '1.1.4'

function Invoke-Dotnet {
	& dotnet @args
	if ($LASTEXITCODE -ne 0) { throw "dotnet $($args -join ' ') failed" }
}

$version = ([xml](Get-Content (Join-Path $root 'Directory.Build.props'))).Project.PropertyGroup.Version
$dist = Join-Path $root 'dist'
$work = Join-Path $dist 'work'
$stage = Join-Path $work "CUShocker-$version"
$zip = Join-Path $dist "CUShocker-$version.zip"
$moduleZip = Join-Path $dist "CUShocker.Module-$version.module.zip"

foreach ($path in @($work, $zip, $moduleZip)) {
	if (Test-Path $path) { Remove-Item $path -Recurse -Force }
}

Invoke-Dotnet test (Join-Path $root 'CUShocker.slnx') -c Release --nologo -v quiet

$modOut = Join-Path $work 'mod'
Invoke-Dotnet build (Join-Path $root 'src\CUShocker.Mod\CUShocker.Mod.csproj') -c Release --nologo -v quiet -o $modOut

# publish also creates the .module.zip
$moduleOut = Join-Path $work 'module'
Invoke-Dotnet publish (Join-Path $root 'src\CUShocker.Module\CUShocker.Module.csproj') -c Release --nologo -v quiet -o $moduleOut
Copy-Item (Join-Path $moduleOut 'CUShocker.Module.module.zip') $moduleZip

$pluginDir = Join-Path $stage 'Casualties Unknown\BepInEx\plugins\CUShocker'
New-Item -ItemType Directory -Force $pluginDir | Out-Null
Copy-Item (Join-Path $modOut 'CUShocker.dll') $pluginDir

$moduleDir = Join-Path $stage 'OpenShock Desktop\modules\my.vore.cushocker'
New-Item -ItemType Directory -Force $moduleDir | Out-Null
Expand-Archive $moduleZip -DestinationPath $moduleDir

$readme = (Get-Content (Join-Path $root 'packaging\README.txt') -Raw).
	Replace('{VERSION}', $version).
	Replace('{DESKTOP_VERSION}', $desktopVersion).
	Replace("`r`n", "`n").Replace("`n", "`r`n")
[IO.File]::WriteAllText((Join-Path $stage 'README.txt'), $readme, (New-Object Text.UTF8Encoding $false))

Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
Remove-Item $work -Recurse -Force

Write-Host "Built $zip"
