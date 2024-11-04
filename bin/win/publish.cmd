:: Copyright (c) Microsoft Corporation.
:: Licensed under the MIT License.

@ECHO OFF

if exist %~dp0\dist (
    echo Removing %~dp0\dist
    rmdir /S /Q %~dp0\dist
)


CALL %~dp0\dotnet.cmd publish %~dp0\..\..\src\AzureAuth\AzureAuth.csproj --self-contained true -p:PublishTrimmed=false -r win-x64 -c release -o %~dp0\..\..\dist %*