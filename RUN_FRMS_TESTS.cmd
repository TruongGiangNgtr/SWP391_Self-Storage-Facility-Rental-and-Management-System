@echo off
setlocal DisableDelayedExpansion
cd /d "%~dp0" || exit /b 2
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0RUN_FRMS_TESTS.ps1" %*
exit /b %ERRORLEVEL%
