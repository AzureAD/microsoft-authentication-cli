:: Copyright (c) Microsoft Corporation.
:: Licensed under the MIT License.

@ECHO OFF
dotnet run --project %~dp0\..\src\AzureAuth -- %* --debug