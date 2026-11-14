@echo off
setlocal EnableExtensions
chcp 65001 >nul
title WindowWatcher 一键编译

set "MODE=build"
set "NOPAUSE="
for %%A in (%*) do (
    if /i "%%~A"=="publish" set "MODE=publish"
    if /i "%%~A"=="/nopause" set "NOPAUSE=1"
)

cd /d "%~dp0"

set "PROJECT=%~dp0\WindowWatcher.csproj"
set "EXE=%~dp0\WindowWatcher\bin\Release\net10.0-windows10.0.19041.0\WindowWatcher.exe"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [错误] 未找到 dotnet 命令，请先安装 .NET 10 SDK。
    echo 下载地址: https://dotnet.microsoft.com/download/dotnet/10.0
    goto :fail
)

if not exist "%PROJECT%" (
    echo [错误] 未找到项目文件：%PROJECT%
    goto :fail
)

if /i "%MODE%"=="publish" goto :publish

echo [1/2] 还原依赖...
dotnet restore "%PROJECT%"
if errorlevel 1 goto :fail

echo [2/2] 编译 Release 版本...
dotnet build "%PROJECT%" -c Release --no-restore --nologo
if errorlevel 1 goto :fail

echo.
echo 编译成功：
echo   %EXE%
goto :done

:publish
echo [1/2] 还原依赖...
dotnet restore "%PROJECT%" -r win-x64
if errorlevel 1 goto :fail

echo [2/2] 发布自包含单文件 exe（win-x64）...
dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:DebugType=None -p:DebugSymbols=false --nologo -o "%~dp0dist"
if errorlevel 1 goto :fail

echo.
echo 发布成功：
echo   %~dp0dist\WindowWatcher.exe
echo 该 exe 可在未安装 .NET 的 Windows 10/11 x64 电脑上直接运行。
goto :done

:fail
echo.
echo 操作失败，请查看上方错误信息。
if not defined NOPAUSE pause
exit /b 1

:done
echo.
if not defined NOPAUSE pause
exit /b 0
