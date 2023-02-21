@ECHO OFF

rem This is a workaround for setting the ADO_TOKEN environment variable to the output of azureauth
FOR /F "tokens=* USEBACKQ" %%F IN (`azureauth --client 872cd9fa-d31f-45e0-9eab-6e460a02d1f1 --scope 499b84ac-1321-427f-aa17-267ca6975798/.default --tenant 72f988bf-86f1-41af-91ab-2d7cd011db47 --output token`) DO (
SET ADO_TOKEN=%%F
)
dotnet %*