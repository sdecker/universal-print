This is a POC to test printing from a ASP.NET 4.8 MVC app to Azure Universal Print. This has been tested to work both in local debug and Azure Web App. Currently it does it's own auth w/ MSAL instead of Azure EasyAuth.

The Entra App Registration config is set in secrets using https://github.com/aspnet/MicrosoftConfigurationBuilders/blob/main/docs/KeyValueConfigBuilders.md#usersecretsconfigbuilder. This includes a few nonSecret values but that is mainly because Visual Studio web.config transforms are abysmally limited. To set the required values for local Dev, right click on the project and choose "Manage User Secrets". Update the secrets.xml with the 4 secrets with the correct values for your environment.

		<secret name="ida:AppID" value="App Registration AppID Guid" />
		<secret name="ida:AppSecret" value="App registration secret" />
		<secret name="ida:TenantId" value="Your Entra Tenant" />
		<secret name="ida:RedirectUri" value="Your apps URL" />

 For running in an Azure Web App, just set the Environment Settings for the Web App to the correct values (grabbing the secret from KV) as per normal.
