@echo off
setlocal

if not "%~1"=="--no-build" if not "%~1"=="--build-if-missing" (
    echo The FoodDiary Development MCP launcher requires --no-build or --build-if-missing. 1>&2
    exit /b 2
)

set "REPOSITORY_ROOT=%~dp0.."
set "SOURCE_DIRECTORY=%REPOSITORY_ROOT%\FoodDiary.Development.Mcp\bin\Debug\net10.0"
set "SOURCE_ASSEMBLY=%SOURCE_DIRECTORY%\FoodDiary.Development.Mcp.dll"
if not exist "%SOURCE_ASSEMBLY%" (
    if "%~1"=="--no-build" (
        echo FoodDiary Development MCP is not built at "%SOURCE_ASSEMBLY%". 1>&2
        exit /b 3
    )
    echo FoodDiary Development MCP output is absent; building it once before startup. 1>&2
    dotnet build "%REPOSITORY_ROOT%\FoodDiary.Development.Mcp\FoodDiary.Development.Mcp.csproj" --nologo --verbosity quiet 1>&2
    if errorlevel 1 exit /b 3
    if not exist "%SOURCE_ASSEMBLY%" (
        echo FoodDiary Development MCP build completed without producing "%SOURCE_ASSEMBLY%". 1>&2
        exit /b 3
    )
)

set "SESSION_ROOT=%TEMP%\fooddiary-development-mcp"
if not exist "%SESSION_ROOT%" mkdir "%SESSION_ROOT%" >nul 2>&1
for /f %%I in ('powershell.exe -NoLogo -NoProfile -NonInteractive -Command "[guid]::NewGuid().ToString('N')"') do set "SESSION_ID=%%I"
set "SESSION_DIRECTORY=%SESSION_ROOT%\%SESSION_ID%"
mkdir "%SESSION_DIRECTORY%" >nul 2>&1
if errorlevel 1 exit /b 4

xcopy "%SOURCE_DIRECTORY%\*" "%SESSION_DIRECTORY%\" /E /I /Q /Y >nul
if errorlevel 1 (
    rmdir /S /Q "%SESSION_DIRECTORY%" >nul 2>&1
    exit /b 5
)

dotnet "%SESSION_DIRECTORY%\FoodDiary.Development.Mcp.dll"
set "SERVER_EXIT_CODE=%ERRORLEVEL%"
rmdir /S /Q "%SESSION_DIRECTORY%" >nul 2>&1
exit /b %SERVER_EXIT_CODE%
