@echo off
cd /d "%~dp0"
if exist "artifacts\Monilivo-v0.18.0\Monilivo.exe" (
    start "" "artifacts\Monilivo-v0.18.0\Monilivo.exe"
) else (
    dotnet run --project src\MonitorDesk -c Release
)
