# Usage

AzureAuth is a generic Azure Active Directory credential provider. It currently supports the following modes of [public client authentication](https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-client-applications) (i.e., authenticating a human user.)
* WAM (Windows Only)
* Embedded Web View (Windows Only)
* System Web Browser (Used on OSX in-place of Embedded Web View)
* Device Code Flow (no GUI, terminal interface only).

## Requirements
You need to [register](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app) your application. You may need to have the following information ready inorder to use the AzureAuth CLI. This information can be found in the Azure portal.
1. A client ID. It is a unique application (client) ID assigned to your app by Azure AD when the app was registered.
2. A resource ID. It is a unique ID representing the resource to which your app needs to be authenticated. 
3. A tenant ID. 

## Shelling out to AzureAuth CLI
"Shelling out" (executing as a subprocess) to AzureAuth CLI is highly recommended to have the best possible authentication experience. 
This insulates your application from potentially lots of dependency headaches, and churn as the authentication libraries used under the hood update, as do the means of authenticating.

Sample python code available [here](python/).

## AzureAuth as a library
If you cannot shell out for any reason, [MSALWrapper](../src/MSALWrapper/) library can be used in your application. Following are the code samples.
1. [Demo.Console.NET6](Demo.Console.NET6/).
2. [Demo.Console.NETFramework472](Demo.Console.NETFramework472/).
