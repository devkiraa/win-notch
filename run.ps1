$env:DOTNET_ROOT = "$env:LOCALAPPDATA\Microsoft\dotnet"
$dotnetExe = "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe"
$dllPath = Join-Path $PSScriptRoot "src\WinNotch\bin\Release\net8.0-windows10.0.19041.0\WinNotch.dll"

Start-Process -FilePath $dotnetExe -ArgumentList "`"$dllPath`""
Write-Host "WinNotch Dynamic Island launched at the top of your screen." -ForegroundColor Green
