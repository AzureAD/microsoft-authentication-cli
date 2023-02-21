:: Copyright (c) Microsoft Corporation.
:: Licensed under the MIT License.

@ECHO OFF
CALL %~dp0\dotnet.cmd run --project %~dp0\..\src\AzureAuth -- %* --debug