@ECHO OFF

rmdir /S /Q %~dp0\dist
dotnet publish %~dp0\..\src\AzureAuth\AzureAuth.csproj --self-contained true -r win10-x64 -c release -o %~dp0\..\dist %*