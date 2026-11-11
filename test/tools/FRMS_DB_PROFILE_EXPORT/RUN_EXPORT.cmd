@echo off
setlocal
cd /d "%~dp0"
py -3 export_frms_profile.py %*
set RESULT=%ERRORLEVEL%
echo.
if not "%RESULT%"=="0" echo Export failed with code %RESULT%.
exit /b %RESULT%
