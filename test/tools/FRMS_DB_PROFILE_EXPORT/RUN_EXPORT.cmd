@echo off
setlocal DisableDelayedExpansion
rem FRMS read-only metadata exporter. Uses a dedicated venv, not system pip.
rem Output: <repo>\test\reports\db-profile
for %%I in ("%~dp0..\..") do set "FRMS_EXPORT_OUT=%%~fI\reports\db-profile"
set "FRMS_EXPORT_VENV=%TEMP%\frms-db-profile-export-venv"
set "FRMS_EXPORT_PY=%FRMS_EXPORT_VENV%\Scripts\python.exe"
cd /d "%~dp0" || exit /b 2

if not exist "%FRMS_EXPORT_PY%" (
    echo Creating isolated Python environment: "%FRMS_EXPORT_VENV%"
    py -3 -m venv "%FRMS_EXPORT_VENV%"
    if errorlevel 1 goto :venv_failed
)
rem Use only the venv Python. System Python 3.14 pip may be corrupted.
"%FRMS_EXPORT_PY%" -m pip --version >nul 2>&1
if errorlevel 1 (
    echo Restoring pip inside the isolated environment...
    "%FRMS_EXPORT_PY%" -m ensurepip --upgrade
    if errorlevel 1 goto :pip_failed
    "%FRMS_EXPORT_PY%" -m pip --version >nul 2>&1
    if errorlevel 1 goto :pip_failed
)

"%FRMS_EXPORT_PY%" -c "import psycopg" >nul 2>&1
if errorlevel 1 (
    echo Installing required PostgreSQL dependency in isolated environment...
    "%FRMS_EXPORT_PY%" -m pip install "psycopg[binary]"
    if errorlevel 1 goto :dependency_failed
)
echo Export directory: "%FRMS_EXPORT_OUT%"
rem A caller-supplied --out (later in argv) overrides this default.
"%FRMS_EXPORT_PY%" export_frms_profile.py --out "%FRMS_EXPORT_OUT%" %*
set "RESULT=%ERRORLEVEL%"
echo.
if not "%RESULT%"=="0" echo Export failed with code %RESULT%.
exit /b %RESULT%

:venv_failed
echo ERROR: Cannot create isolated Python environment. Check "py -3 --version".
exit /b 2

:pip_failed
echo ERROR: pip is unavailable inside "%FRMS_EXPORT_VENV%".
echo Rebuild only this dedicated venv, then run RUN_EXPORT.cmd again.
exit /b 2

:dependency_failed
echo ERROR: psycopg installation failed in the isolated environment.
echo Check network access and Python compatibility; repository and database were not changed by setup.
exit /b 2
