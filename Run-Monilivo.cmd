@echo off
cd /d "%~dp0"
if exist "artifacts\Monilivo-v0.19.2\Monilivo.exe" (
    start "" "artifacts\Monilivo-v0.19.2\Monilivo.exe"
) else (
    dotnet run --project src\MonitorDesk -c Release
)
