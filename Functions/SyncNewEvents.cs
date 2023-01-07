using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CalendarSync.Data;
using CalendarSync.Models;
using CalendarSync.Service.AzureAD;
using CalendarSync.Services.GoogleCloudConsole;

namespace CalendarSync.Functions
{
    public class SyncNewEvents
    {
        private readonly ILogger _logger;
        private List<CalendarEvent>? calendarEvents;

        public string? ConnectionString { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? TenantId { get; set; }
        public string? KeyVaultSecretName { get; set; }
        public string? KeyVaultUri { get; set; }
        public OutlookCalendarService? OutlookCalendarService { get; set; }
        public GoogleCalendarService? GoogleCalendarService { get; set; }
        public string? CalendarId { get; set; }
        public string? UserPrincipalName { get; set; }

        public SyncNewEvents(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SyncNewEvents>();
        }

        [Function("SyncNewEvents")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            HttpResponseData response;

            // Read the values for the new row from the body of the request
            string requestBody = new StreamReader(req.Body).ReadToEnd();

            try
            {

                calendarEvents = JsonConvert.DeserializeObject<List<CalendarEvent>>(requestBody);
            }
            catch (Exception ex)
            {
                // get response from helper method
                response = CreateResponse(req, HttpStatusCode.BadRequest, ex.Message);

                return response;
            }

            // check if list is null or empty
            if (calendarEvents is null || calendarEvents.Count == 0)
            {
                // get response from helper method
                response = CreateResponse(req, HttpStatusCode.BadRequest, "No events were passed");

                return response;
            }

            // Get app settings
            GetAppSettings();

            // confilrm that the app settings were retrieved
            if (String.IsNullOrEmpty(ConnectionString) || String.IsNullOrEmpty(ClientId) || String.IsNullOrEmpty(ClientSecret) || String.IsNullOrEmpty(TenantId) || String.IsNullOrEmpty(KeyVaultSecretName) || String.IsNullOrEmpty(KeyVaultUri) || String.IsNullOrEmpty(CalendarId) || String.IsNullOrEmpty(UserPrincipalName))
            {
                // get response from helper method
                response = CreateResponse(req, HttpStatusCode.BadRequest, "App settings are missing");

                return response;
            }

            // Authenticate to Azure AD and get an access token for Microsoft Graph
            OutlookCalendarService = new OutlookCalendarService();
            var authenticated = await OutlookCalendarService.AuthenticateAzureAdAsync(ClientId, ClientSecret, TenantId);

            // Check if the token was acquired successfully
            if (!authenticated)
            {
                // get response from helper method
                response = CreateResponse(req, HttpStatusCode.Unauthorized, "Unable to authenticate to Azure AD");

                return response;
            }

            // Authenticate to Google Cloud Console and get an access token for Google Calendar
            GoogleCalendarService = new GoogleCalendarService();
            var googleCalendarService = await GoogleCalendarService.AuthenticateGoogleCloudAsync(KeyVaultUri, KeyVaultSecretName);

            // Check if the token was acquired successfully
            if (googleCalendarService is null)
            {
                // get response from helper method
                response = CreateResponse(req, HttpStatusCode.Unauthorized, "Unable to authenticate to Google Cloud Console");

                return response;
            }

            // Use EF Core to insert a record into the table & Apply any pending migrationn
            var context = new AppDbContext();
        
            context.Database.Migrate();
            context.Database.EnsureCreated();

            List<Object> cEvents = new List<Object>();

            foreach(var calendarEvent in calendarEvents)
            {
                var existingCalendarEvent = context?.CalendarEvents?.Find(calendarEvent.Id);
                if (existingCalendarEvent != null)
                {
                    cEvents.Add(new
                    {
                        calendarEvent=existingCalendarEvent,
                        message = "CalendarEvent already exists"
                    });
                    continue;
                }

                context?.CalendarEvents?.Add(calendarEvent);
                context?.SaveChanges();

                calendarEvent.Body = "#meeting";

                // create event in user's calendar
                var newEvent = await GoogleCalendarService.CreateNewEventAsync(calendarEvent, CalendarId);

                // check if event was created successfully
                if (newEvent is null)
                {
                    cEvents.Add(new
                    {
                        calendarEvent=calendarEvent,
                        message = "CalendarEvent was not created in Google Calendar"
                    });

                    continue;
                }

                // update database with google calendar event id
                existingCalendarEvent = context?.CalendarEvents?.Find(calendarEvent.Id);
                if (existingCalendarEvent != null)
                {
                    existingCalendarEvent.PersonalAccEventId = newEvent.Id;
                    context?.SaveChanges();
                }

                cEvents.Add(new
                {
                    calendarEvent=existingCalendarEvent,
                    googleCalendarEvent=newEvent,
                    message = "CalendarEvent created"
                });

            }

            // return the new event
            response = CreateResponse(req, HttpStatusCode.OK, "Succefully Excecuted", cEvents);
            return response;
        }

        private HttpResponseData CreateResponse(HttpRequestData req, HttpStatusCode statusCode, string message, object? data = null)
        {
            var response = req.CreateResponse(statusCode);
            // add json content type to the response
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            // write the error message to the response body
            var responseMessage = new
            {
                Message = message,
                Data = data
            };
            response.WriteString(JsonConvert.SerializeObject(responseMessage));
            return response;
        }

        private void GetAppSettings()
        {
            // Get the connection string from the secrets file and key vault on azure
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            ConnectionString = config.GetConnectionString("DefaultConnection");

            ClientId = config["AzureAd:ClientId"];
            ClientSecret = config["AzureAd:ClientSecret"];
            TenantId = config["AzureAd:TenantId"];
            UserPrincipalName = config["AzureAd:UserPrincipalName"];
            KeyVaultUri= config["AzureKeyVault:KeyVaultUrl"];
            KeyVaultSecretName = config["AzureKeyVault:SecretName"];
            CalendarId = config["GoogleCloudConsole:CalendarId"];
        }
    }
}
