@echo off
cd /d "%~dp0backend\MyStore.Api"
echo Starting MyStore API on http://localhost:5000 ...
dotnet restore
if errorlevel 1 pause & exit /b 1
dotnet run --urls "http://localhost:5000"
pause
