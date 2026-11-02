param([Parameter(Mandatory=$true)][string]$Email)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$password=Read-Host 'Mật khẩu quản trị (tối thiểu 12 ký tự)' -AsSecureString
$pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
try {
    $env:EDUPLAY_ADMIN_EMAIL=$Email
    $env:EDUPLAY_ADMIN_PASSWORD=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    Push-Location $projectRoot
    try {
        & dotnet bin/Release/net10.0/PlayEdu.dll --create-edu-admin
        if($LASTEXITCODE -ne 0){throw 'Admin creation failed. Existing email is never overwritten.'}
    } finally {Pop-Location}
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    Remove-Item Env:EDUPLAY_ADMIN_EMAIL -ErrorAction SilentlyContinue
    Remove-Item Env:EDUPLAY_ADMIN_PASSWORD -ErrorAction SilentlyContinue
}
