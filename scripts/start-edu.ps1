param([int]$Port=5211)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    & dotnet build PlayEdu.csproj -c Release -p:UseAppHost=false
    if($LASTEXITCODE -ne 0){throw 'Build failed. Stop an existing EduPlayV2 process before rebuilding.'}
    & dotnet bin/Release/net10.0/PlayEdu.dll --urls "http://localhost:$Port"
} finally {Pop-Location}
