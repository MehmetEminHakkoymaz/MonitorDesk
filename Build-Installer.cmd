@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-Installer.ps1" %*
set "buildExit=%ERRORLEVEL%"
if not "%buildExit%"=="0" echo Installer build failed. See the error above.
pause
exit /b %buildExit%
