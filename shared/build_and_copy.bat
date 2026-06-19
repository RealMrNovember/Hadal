@echo off
setlocal

set "ROOT=%~dp0"
set "PROJECT=%ROOT%HADAL.Shared\HADAL.Shared.csproj"
set "OUT_DIR=%ROOT%..\Assets\_Hadal\Plugins\Shared"
set "DLL_NAME=HADAL.Shared.dll"

dotnet build "%PROJECT%" -c Release
if errorlevel 1 exit /b 1

if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"

copy /Y "%ROOT%HADAL.Shared\bin\Release\netstandard2.1\%DLL_NAME%" "%OUT_DIR%\%DLL_NAME%"
if errorlevel 1 exit /b 1

echo Copied %DLL_NAME% -^> %OUT_DIR%
