@echo off
setlocal
for %%I in ("%~dp0..") do set "FOODDIARY_REPOSITORY_ROOT=%%~fI"
if not "%~2"=="" for %%I in ("%~2") do set "FOODDIARY_REPOSITORY_ROOT=%%~fI"
set "MCP_ASSEMBLY="
for /f "usebackq delims=" %%I in (`powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0Start-FoodDiaryDevelopmentMcp.ps1" -BuildMode "%~1" -RepositoryRoot "%~2" -PrepareOnly`) do set "MCP_ASSEMBLY=%%I"
if errorlevel 1 exit /b %ERRORLEVEL%
if not defined MCP_ASSEMBLY exit /b 3
for %%I in ("%MCP_ASSEMBLY%") do set "MCP_SESSION=%%~dpI"
set "FOODDIARY_MCP_SESSION_LOCK=%MCP_SESSION%.session.lock"
dotnet "%MCP_ASSEMBLY%"
set "MCP_EXIT_CODE=%ERRORLEVEL%"
exit /b %MCP_EXIT_CODE%
