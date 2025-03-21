using universal_print.Models;
using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using universal_print.TokenStorage;
using Microsoft.Identity.Client;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System;

namespace universal_print.Helpers
{
    public static class GraphHelper
    {
        // Load configuration settings from PrivateSettings.config
        private static string appId = ConfigurationManager.AppSettings["ida:AppId"];
        private static string appSecret = ConfigurationManager.AppSettings["ida:AppSecret"];
        private static string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        private static string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
        private static string authority = "https://login.microsoftonline.com/" + tenantId + "/v2.0";
        private static List<string> graphScopes =
            new List<string>(ConfigurationManager.AppSettings["ida:AppScopes"].Split(' '));


        public static async Task<PrintJob> CreatePrintJobAsync(string printerShareId, HttpPostedFileBase file)
        {
            var graphClient = GetAuthenticatedClient();
            
            // Create a new print job
            var printJob = new PrintJob
            {
                Configuration = new PrintJobConfiguration
                {
                    Orientation = PrintOrientation.Portrait
                }
            };

            // Add the print job to the specified printer share
            var createdPrintJob = await graphClient.Print.Shares[printerShareId].Jobs
                .Request()
                .AddAsync(printJob);

            var printDocument = new PrintDocumentUploadProperties
            {
                DocumentName = file.FileName,
                ContentType = file.ContentType,
                Size = file.ContentLength
            };

            // Upload the document content
            var uploadSession = await graphClient.Print.Shares[printerShareId].Jobs[createdPrintJob.Id].Documents[createdPrintJob.Documents[0].Id].CreateUploadSession(printDocument).Request().PostAsync();
            var maxChunkSize = 320 * 1024; // 320 KB
            var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, file.InputStream, maxChunkSize, graphClient);

            var uploadResult = await fileUploadTask.UploadAsync();
            if (uploadResult.UploadSucceeded)
            {
                var jobStartReturn = await graphClient.Print.Shares[printerShareId].Jobs[createdPrintJob.Id].Start().Request().PostAsync();
                return createdPrintJob;
                
            }
            else
            {
                throw new Exception("File upload failed.");
            }
        }

        public static async Task<IEnumerable<PrinterShare>> GetRegisteredPrinterSharesAsync()
        {
            var graphClient = GetAuthenticatedClient();
            /*
             * STEP 1: List all the universal print shares from graphClient.Print.Shares
             */
            var printerShares = await graphClient.Print.Shares
                .Request()
                .GetAsync();
            return printerShares.CurrentPage;
        }



        public static async Task<IEnumerable<Event>> GetEventsAsync()
        {
            var graphClient = GetAuthenticatedClient();

            var events = await graphClient.Me.Events.Request()
                .Select("subject,organizer,start,end")
                .OrderBy("createdDateTime DESC")
                .GetAsync();

            return events.CurrentPage;
        }

        private static GraphServiceClient GetAuthenticatedClient()
        {
            return new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        var idClient = ConfidentialClientApplicationBuilder.Create(appId)
                            .WithRedirectUri(redirectUri)
                            .WithClientSecret(appSecret)
                            .WithAuthority(authority)
                            .Build();

                        var tokenStore = new SessionTokenStore(idClient.UserTokenCache,
                                HttpContext.Current, ClaimsPrincipal.Current);

                        var userUniqueId = tokenStore.GetUsersUniqueId(ClaimsPrincipal.Current);
                        var account = await idClient.GetAccountAsync(userUniqueId);

                        // By calling this here, the token can be refreshed
                        // if it's expired right before the Graph call is made
                        var result = await idClient.AcquireTokenSilent(graphScopes, account)
                            .ExecuteAsync();

                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", result.AccessToken);
                    }));
        }
        public static async Task<CachedUser> GetUserDetailsAsync(string accessToken)
        {
            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", accessToken);
                    }));

            var user = await graphClient.Me.Request()
                .Select(u => new
                {
                    u.DisplayName,
                    u.Mail,
                    u.UserPrincipalName
                })
                .GetAsync();

            return new CachedUser
            {
                Avatar = string.Empty,
                DisplayName = user.DisplayName,
                Email = string.IsNullOrEmpty(user.Mail) ?
                    user.UserPrincipalName : user.Mail
            };
        }
    }
}