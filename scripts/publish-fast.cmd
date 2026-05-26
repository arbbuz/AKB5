@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-fast.ps1" %*
exit /b %ERRORLEVEL%
