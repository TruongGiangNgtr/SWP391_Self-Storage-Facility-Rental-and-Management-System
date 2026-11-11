@echo off
setlocal DisableDelayedExpansion
rem Default report location: <repo>\test\reports\db-profile
rem Script resides in <repo>\test\tools\FRMS_DB_PROFILE_EXPORT\
for %%I in ("%~dp0..\..") do set "FRMS_EXPORT_OUT=%%~fI\reports\db-profile"
cd /d "%~dp0" || exit /b 2
rem Explicit caller --out supplied after this default takes precedence.
py -3 export_frms_profile.py --out "%FRMS_EXPORT_OUT%" %*
set "RESULT=%ERRORLEVEL%"
echo.
if not "%RESULT%"=="0" echo Export failed with code %RESULT%.
exit /b %RESULT%
