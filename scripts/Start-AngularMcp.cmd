@echo off
setlocal

set "REPOSITORY_ROOT=%~dp0.."
set "CLIENT_ROOT=%REPOSITORY_ROOT%\FoodDiary.Web.Client"
set "ANGULAR_CLI=%CLIENT_ROOT%\node_modules\@angular\cli\bin\ng.js"

if not exist "%ANGULAR_CLI%" (
    echo Angular CLI is not installed at "%ANGULAR_CLI%". Run npm ci in FoodDiary.Web.Client. 1>&2
    exit /b 3
)

cd /d "%CLIENT_ROOT%"
if errorlevel 1 exit /b 4

node "%ANGULAR_CLI%" mcp %*
exit /b %ERRORLEVEL%
