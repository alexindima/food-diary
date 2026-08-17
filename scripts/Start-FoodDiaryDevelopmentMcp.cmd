@echo off
setlocal
powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0Start-FoodDiaryDevelopmentMcp.ps1" -BuildMode "%~1" -RepositoryRoot "%~2"
exit /b %ERRORLEVEL%
