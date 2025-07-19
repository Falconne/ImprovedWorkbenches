@echo off
REM this script checks if PowerShell Core (pwsh) is installed and uses it if available
REM otherwise it falls back to Windows PowerShell (powershell.exe)

where pwsh >nul 2>&1
if %errorlevel%==0 (
    pwsh -ExecutionPolicy Bypass -File "%~1" %2 %3 %4 %5 %6 %7 %8 %9
) else (
    powershell.exe -ExecutionPolicy Bypass -File "%~1" %2 %3 %4 %5 %6 %7 %8 %9
)
