
@echo off
chcp 65001 >nul

cd /d "D:\Unity\Project\ZaoMengXiYou3.Server"

:: 使用开发环境，加载 User Secrets
set "DOTNET_ENVIRONMENT=Development"
set "ASPNETCORE_ENVIRONMENT=Development"

:: 设置服务器地址
set "ASPNETCORE_URLS=http://localhost:5223"

echo ==================================
echo       ZaoMengXiYou3 Server
echo ==================================
echo Starting server...

dotnet run --no-launch-profile

echo.
echo Server stopped.
pause
