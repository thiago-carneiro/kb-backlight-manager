# Copyright 2026 The KB Backlight Manager authors
# SPDX-License-Identifier: Apache-2.0
#
# Tests, then builds the app for x64 and ARM64 with Native AOT into dist\<runtime>.
# Needs the .NET 10 SDK and the Visual Studio C++ build tools (see README).

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

# Native AOT finds the C++ tools with vswhere, which is not on the PATH
$installer = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
if ((Test-Path $installer) -and -not ($env:Path -split ';' -contains $installer)) {
    $env:Path += ";$installer"
}

function Invoke-DotNet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($args -join ' ') failed" }
}

Invoke-DotNet test (Join-Path $PSScriptRoot 'tests\KbBacklightManager.Tests\KbBacklightManager.Tests.csproj') -c Release
foreach ($runtime in 'win-x64', 'win-arm64') {
    $out = Join-Path $PSScriptRoot "dist\$runtime"
    Invoke-DotNet publish (Join-Path $PSScriptRoot 'src\KbBacklightManager\KbBacklightManager.csproj') -c Release -r $runtime -o $out
    Get-Item (Join-Path $out 'KbBacklightManager.exe') | ForEach-Object { '{0} ({1:N1} MB)' -f $_.FullName, ($_.Length / 1MB) }
}
