@echo off
setlocal
cd /d "%~dp0"

set "DOTNET_ROOT=%LOCALAPPDATA%\Microsoft\dotnet"
set "PATH=%LOCALAPPDATA%\Microsoft\dotnet;%PATH%"
set "DLL_PATH=%~dp0src\WinNotch\bin\Release\net8.0-windows10.0.19041.0\WinNotch.dll"

if /i "%1"=="stop" goto do_stop
if /i "%1"=="start" goto do_start
if /i "%1"=="kill" goto do_stop

:menu
cls
echo ========================================================
echo                  WinNotch Controller
echo ========================================================
echo  [1] Start WinNotch
echo  [2] Stop running WinNotch
echo  [3] Fix Smart App Control (Enable Developer Mode)
echo  [4] Exit
echo ========================================================
set /p choice="Select an option (1-4, default 1): "
if "%choice%"=="" set choice=1
if "%choice%"=="1" goto do_start
if "%choice%"=="2" goto do_stop
if "%choice%"=="3" goto do_fix_sac
if "%choice%"=="4" goto :eof
goto menu

:do_stop
echo Stopping any running WinNotch instances...
powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"CommandLine LIKE '%%WinNotch.dll%%'\" | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }" >nul 2>&1
echo WinNotch stopped.
if "%1"=="" (
    pause
    goto menu
)
goto :eof

:do_fix_sac
echo.
echo Opening Windows Settings for Developer Mode...
echo [Toggle Developer Mode to ON to allow local .NET builds]
start ms-settings:developers
echo.
echo Opening Smart App Control settings in Windows Security...
start windowsdefender://appbrowser
echo.
pause
goto menu

:do_start
cls
echo Starting WinNotch...
"%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" "%DLL_PATH%"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo =======================================================================
    echo  ERROR: WinNotch stopped with code %ERRORLEVEL%.
    echo.
    echo  Error 0x800711C7 means Smart App Control blocked the local binary.
    echo  Fix: Switch Smart App Control to 'Off' or 'Evaluation' in Windows Security.
    echo =======================================================================
    echo.
    pause
)
goto :eof
