param([Parameter(Mandatory=$true)][string]$Email)
$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$secret = Read-Host "Mật khẩu Admin (tối thiểu 12 ký tự)" -AsSecureString
$pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
try {
    $env:SABAN_ADMIN_EMAIL = $Email
    $env:SABAN_ADMIN_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    dotnet run --project (Join-Path $projectRoot "PlayEdu.csproj") --no-launch-profile -- --create-admin true
    if ($LASTEXITCODE -ne 0) { throw "Không tạo được tài khoản Admin. Xem lỗi phía trên." }
}
finally {
    Remove-Item Env:SABAN_ADMIN_EMAIL -ErrorAction SilentlyContinue
    Remove-Item Env:SABAN_ADMIN_PASSWORD -ErrorAction SilentlyContinue
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
}

