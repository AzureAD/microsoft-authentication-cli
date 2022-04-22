# Usage
AzureAuth is a generic Azure Active Directory credential provider. It currently supports the following modes of [public client authentication](https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-client-applications) (i.e., authenticating a human user.)
* WAM (Windows Only)
* Embedded Web View (Windows Only)
* System Web Browser (Used on OSX in-place of Embedded Web View)
* Device Code Flow (no GUI, terminal interface only).

## Requirements
This CLI is a "pass-through" for using [MSAL.NET](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet). This means it does not provide any client ID (aka app registration) by default. You must register and configure your own app registration to authenticate with.

### Configure your App Registration
1. You can follow [this quick start guide](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app) to setup your application.
In order to support public client auth modes you must also add redirect URIs under "Mobile and Desktop applications" in the Authentication blade of your app registration in the Azure Portal.
2. To support WAM (the Windows broker), you must add
   ```
   ms-appx-web://microsoft.aad.brokerplugin/<CLIENT_ID>
   ```
3. To support system web browser, you must add
   ```
   http://localhost
   ```
   (Note - do not use `https` here, this is for local redirect and TLS won't work here.)
4. In the bottom of the Authentication Blade, enable the "Allow public client flows" setting.

### Required Arguments to the CLI
You always need to pass at least these three arguments in order to authenticate as something (client id), to something (resource ID), within some AAD tenant. These IDs can be found in the Azure Portal on the Overview of each application/resource/tenant in the AAD section.
1. A client ID. It is a unique application (client) ID assigned to your app by Azure AD when the app was registered.
2. A resource ID. It is a unique ID representing the resource which you want to authenticate to.
3. A tenant ID. (This is found on the main AAD page within the Azure Portal)

## Shelling out to AzureAuth CLI
"Shelling out" (executing as a subprocess) to AzureAuth CLI is highly recommended to have the best possible authentication experience. 
This insulates your application from potentially lots of dependency headaches, and churn as the authentication libraries used under the hood update, as do the means of authenticating.

### Output formats
Use the option `--output` to get the token in the desired formats. Available choices:
1. `--output token` returns token in plain text.
2. `--output json` returns a JSON string of the following format:
    ```json
    {
        "user": "<harryp@hogwarts.com>",
        "display_name": "Harry Potter",
        "token": "<encoded token>",
        "expiration_date": "<expiration date in unix format>"
    }
    ```
3. `--output status` returns the status of the authentication and the cache.
4. `--output none` returns nothing.

Use the option `--help` to understand the other options available when using the CLI. 

### Examples
1. Sample command to authenticate your client to a resource under a tenant. 
    ```
    azureauth --client <clientID> --resource <resourceID> --tenant <tenantID> --output <output format>
    ```
2. Sample python code available [here](../examples/python/).

## AzureAuth as a library
If you cannot shell out for any reason, [MSALWrapper](../src/MSALWrapper/) library can be used in your application. Following are the code samples.
1. [Demo.Console.NET6](../examples/Demo.Console.NET6/).
2. [Demo.Console.NETFramework472](../examples/Demo.Console.NETFramework472/).