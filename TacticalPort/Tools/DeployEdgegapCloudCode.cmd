@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "PAUSE_ON_EXIT=1"

if /I "%~1"=="-NoPause" (
    set "PAUSE_ON_EXIT=0"
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%DeployEdgegapCloudCode.ps1" %*
set "EXIT_CODE=%ERRORLEVEL%"

if "%PAUSE_ON_EXIT%"=="1" (
    echo.
    pause
)

exit /b %EXIT_CODE%
