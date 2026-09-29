@echo off
cd /d "%~dp0"
if exist "artifacts\MonitorDesk-v0.3.0\MonitorDesk.exe" (
    start "" "artifacts\MonitorDesk-v0.3.0\MonitorDesk.exe"
) else (
    dotnet run --project src\MonitorDesk -c Release
)
