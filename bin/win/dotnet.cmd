@ECHO OFF

rem This is a workaround for setting the ADO_TOKEN environment variable to the output of azureauth
FOR /F "tokens=* USEBACKQ" %%F IN (`azureauth ado token --prompt-hint "azureauth dev nuget" --output token`) DO (
SET ADO_TOKEN=%%F
)
dotnet %*