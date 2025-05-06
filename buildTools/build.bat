@echo off
setlocal
cd..
REM === Configuration ===
set PROJECT=FTP_Deploy_Client.csproj
set CONFIGURATION=Release
set RUNTIME=win-x64
set OUTPUT_DIR=publish

REM === Clean output ===
if exist %OUTPUT_DIR% (
    echo Removing existing publish folder...
    rmdir /s /q %OUTPUT_DIR%
)
mkdir %OUTPUT_DIR%

echo.
echo === Building %PROJECT% (%CONFIGURATION%) for %RUNTIME% ===
echo.

dotnet publish %PROJECT% ^
    -c %CONFIGURATION% ^
    -r %RUNTIME% ^
    --self-contained true ^
    /p:PublishSingleFile=true ^
    /p:IncludeNativeLibrariesForSelfExtract=true ^
    /p:DebugType=None /p:DebugSymbols=false ^
    -o %OUTPUT_DIR%


if errorlevel 1 (
    echo Publish failed!
    pause
    exit /b 1
)

echo.
echo === Build and publish completed successfully ===
echo Output is located in: %OUTPUT_DIR%
endlocal
pause
