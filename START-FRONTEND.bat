@echo off
cd /d "%~dp0frontend"
echo Installing frontend packages if needed...
npm install
if errorlevel 1 pause & exit /b 1
echo Starting MyStore frontend...
npm run dev -- --host localhost
pause
