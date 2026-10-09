@echo off
cd /d "%~dp0"
if exist "artifacts\MonitorDesk-v0.10.1\MonitorDesk.exe" (
    start "" "artifacts\MonitorDesk-v0.10.1\MonitorDesk.exe"
) else (
    dotnet run --project src\MonitorDesk -c Release
)
